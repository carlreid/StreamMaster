"use client";

import React, {
	useRef,
	useState,
	useEffect,
	useCallback,
	useMemo,
	type ReactNode,
} from "react";
import { Box, Button, Text, Flex, DialogCloseTrigger } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import {
	DialogActionTrigger,
	DialogBody,
	DialogContent,
	DialogFooter,
	DialogHeader,
	DialogRoot,
	DialogTitle,
	DialogTrigger,
} from "../ui/dialog";
// Import only the types, not the actual module
import type mpegts from "mpegts.js";

interface PlaySMStreamProperties {
	readonly stream: components["schemas"]["SMStreamDto"];
	trigger: ReactNode;
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

export const VideoPlayerDialog = ({
	stream,
	trigger,
}: PlaySMStreamProperties) => {
	const videoRef = useRef<HTMLVideoElement>(null);
	const playerRef = useRef<mpegts.Player | null>(null);
	const [isOpen, setIsOpen] = useState<boolean>(false);
	const [error, setError] = useState<string>("");
	const [isAudioDisabled, setIsAudioDisabled] = useState<boolean>(false);
	const [mediaInfo, setMediaInfo] = useState<MediaInfoDisplay>({});
	const [mpegtsModule, setMpegtsModule] = useState<typeof mpegts | null>(null);
	const [isLoading, setIsLoading] = useState<boolean>(false);

	// Dynamically load mpegts.js
	const loadMpegts = useCallback(async () => {
		if (typeof window === "undefined") return null;

		try {
			setIsLoading(true);
			// Properly type the imported module
			const module = (await import("mpegts.js")) as { default: typeof mpegts };
			setMpegtsModule(module.default);
			setIsLoading(false);
			return module.default;
		} catch (err) {
			console.error("Failed to load mpegts.js:", err);
			setError("Failed to load video player library");
			setIsLoading(false);
			return null;
		}
	}, []);

	const destroyPlayer = useCallback(() => {
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
			setIsAudioDisabled(false);
			setMediaInfo({});
		} catch (e) {
			console.error("Error destroying player:", e);
		}
	}, []);

	const initPlayer = useCallback(
		async (disableAudio = false) => {
			try {
				// Wait for next tick to ensure video element is available
				await new Promise((resolve) => setTimeout(resolve, 0));

				if (!videoRef.current) {
					console.error("No video element found");
					return;
				}

				// Ensure mpegts is loaded
				const mpegts = mpegtsModule || (await loadMpegts());
				if (!mpegts) {
					setError("Failed to load video player");
					return;
				}

				if (!mpegts.isSupported()) {
					setError("Your browser does not support MPEG-TS playback");
					return;
				}

				destroyPlayer();
				setIsAudioDisabled(disableAudio);

				console.log(
					`Initializing mpegts.js player ${
						disableAudio ? "without audio" : "with audio"
					}`,
				);

				playerRef.current = mpegts.createPlayer(
					{
						type: "mpegts",
						url: stream.url,
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
					},
				);

				playerRef.current.attachMediaElement(videoRef.current);

				playerRef.current.on(mpegts.Events.ERROR, (errorType, errorDetail) => {
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
						initPlayer(true); // Reinitialize without audio
						return;
					}

					setError(`Playback Error: ${errorType} - ${errorDetail || ""}`);
				});

				playerRef.current.on(mpegts.Events.STATISTICS_INFO, (stats) => {
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

				playerRef.current.on(mpegts.Events.MEDIA_INFO, (mediaInfo) => {
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

				playerRef.current.load();

				if (videoRef.current) {
					videoRef.current.playsInline = true;
					videoRef.current.autoplay = true;
					videoRef.current.preload = "auto";
					// Set volume to 10%
					videoRef.current.volume = 0.1;
					videoRef.current.setAttribute("webkit-playsinline", "true");
					videoRef.current.setAttribute("x5-playsinline", "true");
					videoRef.current.setAttribute("x5-video-player-type", "h5");
					videoRef.current.setAttribute("x5-video-player-fullscreen", "true");
				}
			} catch (e) {
				console.error("Error initializing player:", e);

				if (e instanceof Error) {
					if (
						!disableAudio &&
						e.message &&
						(e.message.includes("audio") ||
							e.message.includes("MediaSource") ||
							e.message.includes("codec"))
					) {
						console.log("Audio-related error detected, trying without audio");
						initPlayer(true);
						return;
					}

					setError(`Player initialization failed: ${e.message}`);
				}
			}
		},
		[destroyPlayer, stream.url, mpegtsModule, loadMpegts],
	);

	useEffect(() => {
		if (isOpen) {
			console.log("Modal opened - initializing player");
			// Use a small delay to ensure DOM is ready
			const timer = setTimeout(() => {
				initPlayer(false); // Start with audio enabled
			}, 100);
			return () => clearTimeout(timer);
		}
		console.log("Modal closed - destroying player");
		destroyPlayer();
	}, [isOpen, destroyPlayer, initPlayer]);

	// Cleanup on unmount
	useEffect(() => {
		return () => {
			destroyPlayer();
		};
	}, [destroyPlayer]);

	// Format the media info for the title
	const titleMediaInfo = useMemo(() => {
		if (!mediaInfo.videoCodec && !mediaInfo.audioCodec && !mediaInfo.fps) {
			return null;
		}

		const parts = [];

		if (mediaInfo.videoCodec) {
			parts.push(`Video: ${mediaInfo.videoCodec}`);
		}

		if (mediaInfo.audioCodec) {
			parts.push(
				`Audio: ${isAudioDisabled ? "Disabled" : mediaInfo.audioCodec}`,
			);
		}

		if (mediaInfo.fps) {
			parts.push(`${Math.round(mediaInfo.fps)} fps`);
		}

		return parts.join(" | ");
	}, [mediaInfo, isAudioDisabled]);

	return (
		<DialogRoot
			open={isOpen}
			onOpenChange={(e) => setIsOpen(e.open)}
			size="cover"
		>
			<DialogTrigger asChild>{trigger}</DialogTrigger>
			<DialogContent>
				<DialogHeader>
					<DialogTitle>
						<Flex alignItems="center" gap={2}>
							<Text>{stream.name}</Text>
							{titleMediaInfo && (
								<Text fontSize="sm" fontWeight="normal" color="gray.500" ml={2}>
									({titleMediaInfo})
								</Text>
							)}
						</Flex>
					</DialogTitle>
				</DialogHeader>
				<DialogBody position="relative" overflow="hidden">
					<Box width="100%" height="100%" position="relative">
						{/* biome-ignore lint/a11y/useMediaCaption: Sorry a11y */}
						<video
							ref={videoRef}
							controls
							style={{
								backgroundColor: "#000",
								width: "100%",
								height: "100%",
								maxHeight: "100%",
								maxWidth: "100%",
								objectFit: "contain",
							}}
						/>
					</Box>

					{isLoading && (
						<Box
							position="absolute"
							top="50%"
							left="50%"
							transform="translate(-50%, -50%)"
							color="white"
							bg="rgba(0,0,0,0.7)"
							padding="10px 15px"
							borderRadius="4px"
						>
							Loading player...
						</Box>
					)}

					{error && (
						<Box
							position="absolute"
							top="50%"
							left="50%"
							transform="translate(-50%, -50%)"
							color="white"
							bg="rgba(220,53,69,0.8)"
							padding="10px 15px"
							borderRadius="4px"
							maxWidth="80%"
							display="flex"
							flexDirection="column"
							alignItems="center"
						>
							<Text marginBottom="10px">{error}</Text>
							<Button size="sm" onClick={() => setError("")}>
								Dismiss
							</Button>
						</Box>
					)}
				</DialogBody>
				<DialogCloseTrigger />
			</DialogContent>
		</DialogRoot>
	);
};
