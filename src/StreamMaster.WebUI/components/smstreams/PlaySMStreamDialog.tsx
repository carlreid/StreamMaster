import SMPopUp, { type SMPopUpRef } from "@components/sm/SMPopUp";
import type { SMStreamDto } from "@lib/smAPI/smapiTypes";
import React, { useRef, useState, useEffect } from "react";
import mpegts from "mpegts.js";

interface PlaySMStreamProperties {
	readonly smStream: SMStreamDto;
}

const PlaySMStreamDialog = ({ smStream }: PlaySMStreamProperties) => {
	const videoRef = useRef<HTMLVideoElement>(null);
	const playerRef = useRef<mpegts.Player | null>(null);
	const popupRef = useRef<SMPopUpRef>(null);
	const [error, setError] = useState<string>("");

	const destroyPlayer = () => {
		try {
			if (playerRef.current) {
				playerRef.current.pause();
				playerRef.current.unload();
				playerRef.current.detachMediaElement();
				playerRef.current.destroy();
				playerRef.current = null;
			}
			if (videoRef.current) {
				videoRef.current.src = "";
				videoRef.current.load();
			}
			setError("");
		} catch (e) {
			console.error("Error destroying player:", e);
		}
	};

	const initPlayer = () => {
		try {
			if (!videoRef.current) {
				console.error("No video element found");
				return;
			}

			destroyPlayer();

			playerRef.current = mpegts.createPlayer(
				{
					type: "mpegts",
					url: smStream.Url,
					isLive: true,
					hasAudio: true,
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
					rangeLoadZeroStart: false,
					deferLoadAfterSourceOpen: false,
					accurateSeek: false,
				},
			);

			playerRef.current.attachMediaElement(videoRef.current);

			playerRef.current.on(mpegts.Events.ERROR, (errorType, errorDetail) => {
				console.error("MPEGTS Error:", errorType, errorDetail);

				// If error is related to audio codec, try to reinitialize without audio
				if (errorType === "MediaError" && errorDetail?.includes("audio")) {
					console.log("Attempting to play without audio...");
					destroyPlayer();

					if (!videoRef.current) {
						return;
					}

					// Reinitialize player without audio
					playerRef.current = mpegts.createPlayer({
						hasAudio: false,
						isLive: true,
						url: smStream.Url,
						type: "mpegts",
					});

					playerRef.current.attachMediaElement(videoRef.current);
					playerRef.current.load();
					return;
				}

				setError(`Playback Error: ${errorType}`);
			});

			playerRef.current.on(mpegts.Events.STATISTICS_INFO, (stats) => {
				if (stats.speed < 500) {
					console.warn("Low playback speed detected:", stats.speed);
				}
			});

			playerRef.current.on(mpegts.Events.MEDIA_INFO, (mediaInfo) => {
				console.log("Media Info:", mediaInfo);
			});

			playerRef.current.load();

			if (videoRef.current) {
				videoRef.current.playsInline = true;
				videoRef.current.autoplay = true;
				videoRef.current.preload = "auto";
				videoRef.current.setAttribute("webkit-playsinline", "true");
				videoRef.current.setAttribute("x5-playsinline", "true");
				videoRef.current.setAttribute("x5-video-player-type", "h5");
				videoRef.current.setAttribute("x5-video-player-fullscreen", "true");
			}
			// biome-ignore lint/suspicious/noExplicitAny: <explanation>
		} catch (e: any) {
			console.error("Error initializing player:", e);
			setError(`Player initialization failed: ${e.message}`);
		}
	};

	// biome-ignore lint/correctness/useExhaustiveDependencies: <explanation>
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
				{/* biome-ignore lint/a11y/useMediaCaption: <explanation> */}
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
				{error && (
					<div
						style={{
							position: "absolute",
							top: "50%",
							left: "50%",
							transform: "translate(-50%, -50%)",
							color: "red",
							background: "rgba(0,0,0,0.7)",
							padding: "10px",
						}}
					>
						{error}
					</div>
				)}
			</div>
		</SMPopUp>
	);
};

PlaySMStreamDialog.displayName = "PlaySMStreamDialog";

export default React.memo(PlaySMStreamDialog);
