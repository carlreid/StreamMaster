import SMPopUp, { type SMPopUpRef } from "@components/sm/SMPopUp";
import type { SMStreamDto } from "@lib/smAPI/smapiTypes";
import React, { useRef, useState, useEffect } from "react";
import Hls from "hls.js";
import mpegts from "mpegts.js";

interface PlaySMStreamProperties {
  readonly smStream: SMStreamDto;
}

interface MediaInfoDisplay {
  videoCodec?: string;
  audioCodec?: string;
  width?: number;
  height?: number;
  fps?: number;
  videoBitrate?: number;
  audioBitrate?: number;
}

// Stream type detection
enum StreamType {
  UNKNOWN = "unknown",
  HLS = "hls",
  MPEGTS = "mpegts",
}

const PlaySMStreamDialog = ({ smStream }: PlaySMStreamProperties) => {
  const videoRef = useRef<HTMLVideoElement>(null);
  const hlsRef = useRef<Hls | null>(null);
  const mpegtsPlayerRef = useRef<mpegts.Player | null>(null);
  const popupRef = useRef<SMPopUpRef>(null);
  const [error, setError] = useState<string>("");
  const [isAudioDisabled, setIsAudioDisabled] = useState<boolean>(false);
  const [mediaInfo, setMediaInfo] = useState<MediaInfoDisplay>({});
  const [playerType, setPlayerType] = useState<StreamType>(StreamType.UNKNOWN);

  const destroyPlayer = () => {
    try {
      // Destroy HLS player if exists
      if (hlsRef.current) {
        hlsRef.current.destroy();
        hlsRef.current = null;
      }

      // Destroy MPEGTS player if exists
      if (mpegtsPlayerRef.current) {
        mpegtsPlayerRef.current.pause();
        mpegtsPlayerRef.current.unload();
        mpegtsPlayerRef.current.detachMediaElement();
        mpegtsPlayerRef.current.destroy();
        mpegtsPlayerRef.current = null;
      }

      if (videoRef.current) {
        videoRef.current.src = "";
        videoRef.current.load();
      }

      setError("");
      setIsAudioDisabled(false);
      setMediaInfo({});
      setPlayerType(StreamType.UNKNOWN);
    } catch (e) {
      console.error("Error destroying player:", e);
    }
  };

  const dismissError = () => {
    setError("");
  };

  // Detect stream type by checking the URL or making a HEAD request
  const detectStreamType = async (url: string): Promise<StreamType> => {
    // First check URL pattern
    if (url.includes('.m3u8')) {
      return StreamType.HLS;
    } else if (url.includes('.ts')) {
      return StreamType.MPEGTS;
    }

    // If URL doesn't give us a clue, try to fetch the first few bytes
    try {
      const response = await fetch(url, {
        method: 'HEAD',
        headers: { 'Range': 'bytes=0-10' }
      });

      const contentType = response.headers.get('content-type');

      if (contentType) {
        if (contentType.includes('application/vnd.apple.mpegurl') || 
            contentType.includes('application/x-mpegurl')) {
          return StreamType.HLS;
        } else if (contentType.includes('video/mp2t')) {
          return StreamType.MPEGTS;
        }
      }

      // If HEAD request doesn't work, try a small GET request
      const dataResponse = await fetch(url, {
        method: 'GET',
        headers: { 'Range': 'bytes=0-200' }
      });

      const text = await dataResponse.text();

      if (text.startsWith('#EXTM3U')) {
        return StreamType.HLS;
      }

      // Default to MPEGTS if we can't determine
      return StreamType.MPEGTS;
    } catch (e) {
      console.error("Error detecting stream type:", e);
      return StreamType.MPEGTS; // Default to MPEGTS
    }
  };

  const initHlsPlayer = () => {
    if (!videoRef.current) return;

    // Check if HLS is supported natively (like on Safari)
    if (videoRef.current.canPlayType("application/vnd.apple.mpegurl")) {
      console.log("Using native HLS support");
      videoRef.current.src = smStream.Url;
      videoRef.current.addEventListener("loadedmetadata", () => {
        if (videoRef.current) {
          videoRef.current.play().catch(e => {
            console.error("Error playing video:", e);
            setError(`Playback Error: ${e.message}`);
          });
        }
      });
    } 
    // Use HLS.js if the browser doesn't support HLS natively
    else if (Hls.isSupported()) {
      console.log("Using HLS.js for playback");
      hlsRef.current = new Hls({
        maxBufferLength: 30,
        maxMaxBufferLength: 60,
        liveSyncDuration: 3,
        liveMaxLatencyDuration: 10,
        enableWorker: true,
      });

      hlsRef.current.attachMedia(videoRef.current);

      hlsRef.current.on(Hls.Events.MEDIA_ATTACHED, () => {
        console.log("HLS media attached");
        hlsRef.current?.loadSource(smStream.Url);
      });

      hlsRef.current.on(Hls.Events.MANIFEST_PARSED, (event, data) => {
        console.log("HLS manifest parsed:", data);
        videoRef.current?.play().catch(e => {
          console.error("Error playing video:", e);
          setError(`Playback Error: ${e.message}`);
        });

        // Update media info
        setMediaInfo({
          width: data.levels[data.currentLevel]?.width,
          height: data.levels[data.currentLevel]?.height,
          videoBitrate: data.levels[data.currentLevel]?.bitrate,
          videoCodec: "H.264", // Most HLS streams use H.264
          audioCodec: "AAC",   // Most HLS streams use AAC
        });
      });

      hlsRef.current.on(Hls.Events.ERROR, (event, data) => {
        console.error("HLS Error:", data);
        if (data.fatal) {
          switch (data.type) {
            case Hls.ErrorTypes.NETWORK_ERROR:
              console.log("Network error, trying to recover...");
              hlsRef.current?.startLoad();
              break;
            case Hls.ErrorTypes.MEDIA_ERROR:
              console.log("Media error, trying to recover...");
              hlsRef.current?.recoverMediaError();
              break;
            default:
              setError(`Playback Error: ${data.type} - ${data.details}`);
              destroyPlayer();
              break;
          }
        }
      });

      // Update bitrate info
      hlsRef.current.on(Hls.Events.LEVEL_SWITCHED, (event, data) => {
        const level = hlsRef.current?.levels[data.level];
        if (level) {
          setMediaInfo(prev => ({
            ...prev,
            width: level.width,
            height: level.height,
            videoBitrate: level.bitrate,
          }));
        }
      });
    } else {
      setError("Your browser does not support HLS playback");
    }
  };

  const initMpegtsPlayer = (disableAudio = false) => {
    try {
      if (!videoRef.current) {
        console.error("No video element found");
        return;
      }

      if (!mpegts.isSupported()) {
        setError("Your browser does not support MPEG-TS playback");
        return;
      }

      setIsAudioDisabled(disableAudio);

      console.log(
        `Initializing mpegts.js player ${disableAudio ? "without audio" : "with audio"}`,
      );

      mpegtsPlayerRef.current = mpegts.createPlayer(
        {
          type: "mpegts",
          url: smStream.Url,
          isLive: true,
          hasAudio: !disableAudio,
          hasVideo: true,
        },
        {
          enableWorker: true,
          lazyLoad: false,
          liveBufferLatencyChasing: true,
          fixAudioTimestampGap: true,
          seekType: "range",
          reuseRedirectedURL: true,
          liveBufferLatencyMaxLatency: 3.0,
          liveBufferLatencyMinRemain: 0.5,
          videoCodec: "avc", // H.264
          audioCodec: disableAudio ? undefined : "aac", // Try with AAC instead of AC-3
        },
      );

      mpegtsPlayerRef.current.attachMediaElement(videoRef.current);

      mpegtsPlayerRef.current.on(mpegts.Events.ERROR, (errorType, errorDetail) => {
        console.error("MPEGTS Error:", errorType, errorDetail);

        // If error is related to audio codec or MSE, try to reinitialize without audio
        if (
          !disableAudio &&
          (errorType === "MediaError" ||
            errorDetail?.includes("audio") ||
            errorDetail?.includes("ac-3") ||
            errorDetail?.includes("Can't play type"))
        ) {
          console.log(
            "Audio codec error detected, attempting to play without audio...",
          );
          destroyPlayer();
          setPlayerType(StreamType.MPEGTS);
          initMpegtsPlayer(true); // Reinitialize without audio
          return;
        }

        // If we get an unsupported format error, try HLS instead
        if (errorType === "FormatUnsupported" || errorDetail?.includes("Unsupported media type")) {
          console.log("Format unsupported, trying HLS player instead");
          destroyPlayer();
          setPlayerType(StreamType.HLS);
          initHlsPlayer();
          return;
        }

        setError(`Playback Error: ${errorType} - ${errorDetail || ""}`);
      });

      mpegtsPlayerRef.current.on(mpegts.Events.STATISTICS_INFO, (stats) => {
        if (stats.speed < 500) {
          console.warn("Low playback speed detected:", stats.speed);
        }

        // Update media info with statistics
        setMediaInfo((prevInfo) => ({
          ...prevInfo,
          videoBitrate: stats.videoBitrate,
          audioBitrate: stats.audioBitrate,
        }));
      });

      mpegtsPlayerRef.current.on(mpegts.Events.MEDIA_INFO, (mediaInfo) => {
        console.log("Media Info:", mediaInfo);

        // Extract and display codec information
        setMediaInfo({
          videoCodec: mediaInfo.videoCodec,
          audioCodec: disableAudio
            ? "Disabled (AC-3 not supported)"
            : mediaInfo.audioCodec,
          width: mediaInfo.width,
          height: mediaInfo.height,
          fps: mediaInfo.fps,
          videoBitrate: mediaInfo.videoBitrate,
          audioBitrate: mediaInfo.audioBitrate,
        });
      });

      mpegtsPlayerRef.current.load();

      if (videoRef.current) {
        videoRef.current.playsInline = true;
        videoRef.current.autoplay = true;
        videoRef.current.preload = "auto";
        videoRef.current.setAttribute("webkit-playsinline", "true");
        videoRef.current.setAttribute("x5-playsinline", "true");
        videoRef.current.setAttribute("x5-video-player-type", "h5");
        videoRef.current.setAttribute("x5-video-player-fullscreen", "true");
      }
    } catch (e: any) {
      console.error("Error initializing MPEGTS player:", e);

      if (
        !disableAudio &&
        e.message &&
        (e.message.includes("audio") ||
          e.message.includes("MediaSource") ||
          e.message.includes("codec"))
      ) {
        console.log("Audio-related error detected, trying without audio");
        initMpegtsPlayer(true);
        return;
      }

      setError(`Player initialization failed: ${e.message}`);
    }
  };

  const initPlayer = async () => {
    try {
      if (!videoRef.current) {
        console.error("No video element found");
        return;
      }

      destroyPlayer();

      // Detect stream type
      const streamType = await detectStreamType(smStream.Url);
      setPlayerType(streamType);

      console.log(`Detected stream type: ${streamType}`);

      if (streamType === StreamType.HLS) {
        initHlsPlayer();
      } else {
        initMpegtsPlayer(false); // Start with audio enabled
      }

      if (videoRef.current) {
        videoRef.current.playsInline = true;
        videoRef.current.autoplay = true;
        videoRef.current.preload = "auto";
        videoRef.current.setAttribute("webkit-playsinline", "true");
        videoRef.current.setAttribute("x5-playsinline", "true");
        videoRef.current.setAttribute("x5-video-player-type", "h5");
        videoRef.current.setAttribute("x5-video-player-fullscreen", "true");
      }
    } catch (e: any) {
      console.error("Error initializing player:", e);
      setError(`Player initialization failed: ${e.message}`);
    }
  };

  useEffect(() => {
    let lastState = false;
    const interval = setInterval(() => {
      const isOpen = popupRef.current?.getOpen() || false;

      if (isOpen !== lastState) {
        console.log("Modal state changed:", isOpen);
        lastState = isOpen;

        if (isOpen) {
          console.log("Modal opened - initializing player");
          initPlayer();
        } else {
          console.log("Modal closed - destroying player");
          destroyPlayer();
        }
      }
    }, 100);

    return () => {
      clearInterval(interval);
      destroyPlayer();
    };
  }, []);

  return (
    <SMPopUp
      ref={popupRef}
      buttonClassName="icon-green"
      buttonDisabled={!smStream}
      title="Play Stream"
      icon="pi-play"
      contentWidthSize="8"
      modal
      modalClosable
      showClose={false}
      info=""
      modalCentered
      noBorderChildren
      zIndex={11}
    >
      <div style={{ width: "100%", height: "100%", position: "relative" }}>
        <video
          ref={videoRef}
          controls
          style={{
            width: "100%",
            height: "100%",
            minHeight: "360px",
            backgroundColor: "#000",
          }}
        />

        {/* Dismissible Error Message */}
        {error && (
          <div
            style={{
              position: "absolute",
              top: "50%",
              left: "50%",
              transform: "translate(-50%, -50%)",
              color: "white",
              background: "rgba(220,53,69,0.8)",
              padding: "10px 15px",
              borderRadius: "4px",
              maxWidth: "80%",
              display: "flex",
              flexDirection: "column",
              alignItems: "center",
            }}
          >
            <div style={{ marginBottom: "10px" }}>{error}</div>
            <button
              onClick={dismissError}
              style={{
                background: "rgba(255,255,255,0.2)",
                border: "1px solid rgba(255,255,255,0.4)",
                color: "white",
                padding: "5px 10px",
                borderRadius: "3px",
                cursor: "pointer",
                fontSize: "12px",
              }}
            >
              Dismiss
            </button>
          </div>
        )}

        {/* Media Info Display */}
        <div
          style={{
            position: "absolute",
            top: "10px",
            left: "10px",
            background: "rgba(0,0,0,0.6)",
            color: "white",
            padding: "8px",
            borderRadius: "4px",
            fontSize: "12px",
            maxWidth: "300px",
          }}
        >
          <div style={{ fontWeight: "bold", marginBottom: "5px" }}>
            Player: {playerType === StreamType.HLS 
              ? (Hls.isSupported() ? "HLS.js" : "Native HLS") 
              : "mpegts.js"}
          </div>

          {mediaInfo.videoCodec && (
            <div>
              <span style={{ color: "#8af" }}>Video:</span>{" "}
              {mediaInfo.videoCodec}
              {mediaInfo.width &&
                mediaInfo.height &&
                ` (${mediaInfo.width}×${mediaInfo.height})`}
              {mediaInfo.fps && ` ${Math.round(mediaInfo.fps)} fps`}
              {mediaInfo.videoBitrate &&
                ` ${Math.round(mediaInfo.videoBitrate / 1000)} kbps`}
            </div>
          )}

          <div>
            <span style={{ color: "#8af" }}>Audio:</span>{" "}
            {isAudioDisabled
              ? "Disabled (AC-3 not supported)"
              : mediaInfo.audioCodec || "Detecting..."}
            {!isAudioDisabled &&
              mediaInfo.audioBitrate &&
              ` ${Math.round(mediaInfo.audioBitrate / 1000)} kbps`}
          </div>
        </div>
      </div>
    </SMPopUp>
  );
};

PlaySMStreamDialog.displayName = "PlaySMStreamDialog";

export default React.memo(PlaySMStreamDialog);
