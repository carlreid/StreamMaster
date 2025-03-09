"use client";

import { useEffect, useState } from "react";
import {
	Box,
	Flex,
	Heading,
	Text,
	VStack,
	Badge,
	Card,
	Separator,
	Image,
	Spinner,
	Stat,
	Button,
	IconButton,
	HStack,
	useDisclosure,
	Dialog,
	Portal,
} from "@chakra-ui/react";
import { useSignalR } from "../../lib/use-signalr";
import { apiClient } from "../../lib/api";
import { toaster } from "../ui/toaster";
import type { components } from "../../lib/api.d";
import { formatBytes, formatDuration } from "../../lib/utils";
import { Tooltip } from "../ui/tooltip";
import { LuArrowRight, LuX } from "react-icons/lu";

enum SMStreamTypeEnum {
	Regular = 0,
	CustomPlayList = 1,
	Custom = 2,
	Intro = 3,
	Message = 4,
}

const formatStreamType = (type: SMStreamTypeEnum) => {
	switch (type) {
		case SMStreamTypeEnum.Regular:
			return "Regular";
		case SMStreamTypeEnum.CustomPlayList:
			return "Custom Playlist";
		case SMStreamTypeEnum.Custom:
			return "Custom";
		case SMStreamTypeEnum.Intro:
			return "Intro";
		case SMStreamTypeEnum.Message:
			return "Message";
		default:
			return "Unknown";
	}
};

const ChannelCard = ({
	channel,
	isSelected,
	onClick,
	onCancelChannel,
	onMoveToNextStream,
}: {
	channel: components["schemas"]["ChannelMetric"];
	isSelected: boolean;
	onClick: () => void;
	onCancelChannel: () => void;
	onMoveToNextStream: () => void;
}) => {
	return (
		<Card.Root
			variant={isSelected ? "elevated" : "subtle"}
			cursor="pointer"
			onClick={onClick}
			mb={2}
			_hover={{ shadow: "md" }}
		>
			<Card.Header pb={2}>
				<Flex align="center" gap={2}>
					{channel.channelLogo && (
						<Image
							src={channel.channelLogo}
							alt={channel.name}
							boxSize="24px"
							borderRadius="sm"
						/>
					)}
					<Heading size="sm">{channel.name}</Heading>
					{channel.isFailed && (
						<Badge colorPalette="red" ml="auto">
							Failed
						</Badge>
					)}
				</Flex>
			</Card.Header>
			<Card.Body pt={0}>
				<Text fontSize="xs" color="gray.500">
					Source: {channel.sourceName}
				</Text>
				<Flex mt={2} gap={3} wrap="wrap">
					<Stat.Root>
						<Stat.Label fontSize="2xs">Data</Stat.Label>
						<Stat.ValueText fontSize="xs">
							{formatBytes(channel.metrics.bytesRead)}
						</Stat.ValueText>
					</Stat.Root>

					<Stat.Root>
						<Stat.Label fontSize="2xs">Speed</Stat.Label>
						<Stat.ValueText fontSize="xs">
							{Math.round(channel.metrics.kbps)}{" "}
							<Stat.ValueUnit>kbps</Stat.ValueUnit>
						</Stat.ValueText>
					</Stat.Root>

					<Stat.Root>
						<Stat.Label fontSize="2xs">Errors</Stat.Label>
						<Stat.ValueText
							fontSize="xs"
							color={channel.metrics.errorCount > 0 ? "red.500" : "inherit"}
						>
							{channel.metrics.errorCount}
						</Stat.ValueText>
					</Stat.Root>
				</Flex>

				{isSelected && (
					<HStack mt={2} gap={2} onClick={(e) => e.stopPropagation()}>
						<Tooltip content="Cancel Channel">
							<IconButton
								aria-label="Cancel Channel"
								size="xs"
								colorPalette="red"
								variant="outline"
								onClick={onCancelChannel}
							>
								<LuX />
							</IconButton>
						</Tooltip>
						<Tooltip content="Move to Next Stream">
							<IconButton
								aria-label="Move to Next Stream"
								size="xs"
								colorPalette="blue"
								variant="outline"
								onClick={onMoveToNextStream}
							>
								<LuArrowRight />
							</IconButton>
						</Tooltip>
					</HStack>
				)}
			</Card.Body>
		</Card.Root>
	);
};

const ClientStreamDetails = ({
	clientStreams,
	onCancelClient,
}: {
	clientStreams: components["schemas"]["ClientStreamsDto"][];
	channelInfo: components["schemas"]["ChannelMetric"];
	onCancelClient: (clientId: string) => void;
}) => {
	if (clientStreams.length === 0) {
		return (
			<Box p={4} textAlign="center">
				<Text color="gray.500">No active client streams</Text>
			</Box>
		);
	}

	return (
		<Box>
			{clientStreams.map((client) => (
				<Card.Root key={client.smStreamId} mb={3} variant="outline">
					<Card.Header pb={1}>
						<Flex justify="space-between" align="center">
							<Tooltip content={client.clientUserAgent || "Unknown"}>
								<Heading size="xs">{client.name}</Heading>
							</Tooltip>
							<Flex align="center" gap={2}>
								<Text fontSize="xs" color="gray.500">
									{client.clientIPAddress || "Unknown"}
								</Text>
								<Tooltip content="Cancel Client Stream">
									<IconButton
										aria-label="Cancel Client Stream"
										size="xs"
										colorPalette="red"
										variant="outline"
										onClick={() => onCancelClient(client.smStreamId)}
									>
										<LuX />
									</IconButton>
								</Tooltip>
							</Flex>
						</Flex>
					</Card.Header>
					<Card.Body pt={1}>
						<Flex wrap="wrap" gap={3}>
							<Stat.Root>
								<Stat.Label>Data</Stat.Label>
								<Stat.ValueText>
									{client.metrics
										? formatBytes(client.metrics.bytesWritten)
										: "N/A"}
								</Stat.ValueText>
							</Stat.Root>

							<Stat.Root>
								<Stat.Label>Speed</Stat.Label>
								<Stat.ValueText>
									{client.metrics ? Math.round(client.metrics.kbps) : "N/A"}
									<Stat.ValueUnit>kbps</Stat.ValueUnit>
								</Stat.ValueText>
							</Stat.Root>

							<Stat.Root>
								<Stat.Label>Duration</Stat.Label>
								<Stat.ValueText>
									{(client.metrics &&
										formatDuration(client.metrics.startTime)) ||
										"N/A"}
								</Stat.ValueText>
							</Stat.Root>
						</Flex>
					</Card.Body>
				</Card.Root>
			))}
		</Box>
	);
};

const SourceStreamDetails = ({
	channel,
}: {
	channel: components["schemas"]["ChannelMetric"];
}) => {
	if (!channel.smStreamInfo) {
		return (
			<Box p={4} textAlign="center">
				<Text>No source stream information available</Text>
			</Box>
		);
	}

	const { smStreamInfo, metrics } = channel;

	return (
		<VStack align="stretch" gap={3} p={2}>
			<Stat.Root size="sm">
				<Stat.Label>URL</Stat.Label>
				<Stat.ValueText>{smStreamInfo.url}</Stat.ValueText>
			</Stat.Root>

			<Flex wrap="wrap" gap={4}>
				<Stat.Root size="sm">
					<Stat.Label>Type</Stat.Label>
					<Stat.ValueText>
						{formatStreamType(smStreamInfo.smStreamType)}
					</Stat.ValueText>
				</Stat.Root>

				<Stat.Root size="sm">
					<Stat.Label>Duration</Stat.Label>
					<Stat.ValueText>
						{smStreamInfo.secondsIn} <Stat.ValueUnit>seconds</Stat.ValueUnit>
					</Stat.ValueText>
				</Stat.Root>
			</Flex>

			<Stat.Root size="sm">
				<Stat.Label>Command</Stat.Label>
				<Stat.ValueText>
					{smStreamInfo.commandProfile?.command || "N/A"}
				</Stat.ValueText>
			</Stat.Root>

			<Flex wrap="wrap" gap={4}>
				<Stat.Root size="sm">
					<Stat.Label>Data Read</Stat.Label>
					<Stat.ValueText>{formatBytes(metrics.bytesRead)}</Stat.ValueText>
				</Stat.Root>

				<Stat.Root size="sm">
					<Stat.Label>Data Written</Stat.Label>
					<Stat.ValueText>{formatBytes(metrics.bytesWritten)}</Stat.ValueText>
				</Stat.Root>

				<Stat.Root size="sm">
					<Stat.Label>Bitrate</Stat.Label>
					<Stat.ValueText>
						{Math.round(metrics.kbps)} <Stat.ValueUnit>kbps</Stat.ValueUnit>
					</Stat.ValueText>
				</Stat.Root>

				<Stat.Root size="sm">
					<Stat.Label>Latency</Stat.Label>
					<Stat.ValueText>
						{metrics.averageLatency.toFixed(2)}{" "}
						<Stat.ValueUnit>ms</Stat.ValueUnit>
					</Stat.ValueText>
				</Stat.Root>

				<Stat.Root size="sm">
					<Stat.Label>Errors</Stat.Label>
					<Stat.ValueText
						color={metrics.errorCount > 0 ? "red.500" : "inherit"}
					>
						{metrics.errorCount}
					</Stat.ValueText>
				</Stat.Root>
			</Flex>

			{channel.videoInfo && (
				<Box>
					<Text>Video Info</Text>
					<Text fontSize="sm">{channel.videoInfo}</Text>
				</Box>
			)}
		</VStack>
	);
};

export const StreamingStatus = () => {
	const signalR = useSignalR();
	const [channelMetrics, setChannelMetrics] = useState<
		components["schemas"]["ChannelMetric"][]
	>([]);
	const [isLoading, setIsLoading] = useState(true);
	const [selectedChannelId, setSelectedChannelId] = useState<string | null>(
		null,
	);
	const [refreshInterval, setRefreshInterval] = useState<NodeJS.Timeout | null>(
		null,
	);
	const [isProcessing, setIsProcessing] = useState(false);

	// Confirmation dialog states
	const cancelAllDialog = useDisclosure();
	const cancelChannelDialog = useDisclosure();
	const cancelClientDialog = useDisclosure();
	const [clientToCancel, setClientToCancel] = useState<string | null>(null);

	const selectedChannel =
		channelMetrics.find((c) => c.id === selectedChannelId) || null;

	const loadChannelMetrics = async () => {
		try {
			let data: components["schemas"]["ChannelMetric"][] = [];

			if (signalR.isConnected) {
				data = await signalR.invokeWithRefresh(
					"GetChannelMetrics",
					async (data: Promise<components["schemas"]["ChannelMetric"][]>) => {
						setChannelMetrics(await data);
					},
				);
			} else {
				// Fallback to REST API
				const response = await apiClient.GET(
					"/api/statistics/getchannelmetrics",
				);
				if (response.data) {
					data = response.data;
				}
			}

			setChannelMetrics(data);

			// Select first channel if none is selected
			if (!selectedChannelId && data.length > 0) {
				const id = data[0]?.id;
				if (id) {
					setSelectedChannelId(id);
				}
			}

			setIsLoading(false);
		} catch (error) {
			console.error("Failed to load channel metrics:", error);
			toaster.create({
				title: "Error",
				description: "Failed to load streaming status",
				type: "error",
			});
			setIsLoading(false);
		}
	};

	// Handle canceling all channels
	const handleCancelAllChannels = async () => {
		setIsProcessing(true);
		try {
			if (signalR.isConnected) {
				await signalR.invoke("CancelAllChannels");
			} else {
				await apiClient.PATCH("/api/streaming/cancelallchannels");

				toaster.create({
					title: "Success",
					description: "All channels have been canceled",
					type: "success",
				});
			}

			// Refresh the data
			await loadChannelMetrics();
		} catch (error) {
			console.error("Failed to cancel all channels:", error);
			toaster.create({
				title: "Error",
				description: "Failed to cancel all channels",
				type: "error",
			});
		} finally {
			setIsProcessing(false);
			cancelAllDialog.onClose();
		}
	};

	// Handle canceling a specific channel
	const handleCancelChannel = async () => {
		if (!selectedChannelId) return;

		setIsProcessing(true);
		try {
			const request: components["schemas"]["CancelChannelRequest"] = {
				smChannelId: Number.parseInt(selectedChannelId),
			};

			if (signalR.isConnected) {
				await signalR.invoke("CancelChannel", request);
			} else {
				await apiClient.PATCH("/api/streaming/cancelchannel", {
					body: request,
				});
				toaster.create({
					title: "Success",
					description: "Channel has been canceled",
					type: "success",
				});
			}

			// Refresh the data
			await loadChannelMetrics();
		} catch (error) {
			console.error("Failed to cancel channel:", error);
			toaster.create({
				title: "Error",
				description: "Failed to cancel channel",
				type: "error",
			});
		} finally {
			setIsProcessing(false);
			cancelChannelDialog.onClose();
		}
	};

	// Handle canceling a specific client
	const handleCancelClient = async () => {
		if (!clientToCancel || !selectedChannelId) return;

		setIsProcessing(true);
		try {
			const request: components["schemas"]["CancelClientRequest"] = {
				uniqueRequestId: clientToCancel,
			};

			if (signalR.isConnected) {
				await signalR.invoke("CancelClient", request);
			} else {
				await apiClient.PATCH("/api/streaming/cancelclient", {
					body: request,
				});
				toaster.create({
					title: "Success",
					description: "Client stream has been canceled",
					type: "success",
				});
			}

			// Refresh the data
			await loadChannelMetrics();
		} catch (error) {
			console.error("Failed to cancel client:", error);
			toaster.create({
				title: "Error",
				description: "Failed to cancel client stream",
				type: "error",
			});
		} finally {
			setIsProcessing(false);
			setClientToCancel(null);
			cancelClientDialog.onClose();
		}
	};

	// Handle moving to next stream
	const handleMoveToNextStream = async () => {
		if (!selectedChannelId) return;

		setIsProcessing(true);
		try {
			const request: components["schemas"]["MoveToNextStreamRequest"] = {
				smChannelId: Number.parseInt(selectedChannelId),
			};

			if (signalR.isConnected) {
				await signalR.invoke("MoveToNextStream", request);
			} else {
				await apiClient.PATCH("/api/streaming/movetonextstream", {
					body: request,
				});
				toaster.create({
					title: "Success",
					description: "Moving to next stream",
					type: "success",
				});
			}

			// Refresh the data
			await loadChannelMetrics();
		} catch (error) {
			console.error("Failed to move to next stream:", error);
			toaster.create({
				title: "Error",
				description: "Failed to move to next stream",
				type: "error",
			});
		} finally {
			setIsProcessing(false);
		}
	};

	// biome-ignore lint/correctness/useExhaustiveDependencies: Want to load channel metrics on mount
	useEffect(() => {
		loadChannelMetrics();

		// Set up refresh interval
		const interval = setInterval(loadChannelMetrics, 5000);
		setRefreshInterval(interval);

		return () => {
			if (refreshInterval) {
				clearInterval(refreshInterval);
			}
		};
	}, [signalR.isConnected]);

	return (
		<Box>
			{isLoading ? (
				<Flex justify="center" align="center" h="300px">
					<Spinner />
				</Flex>
			) : channelMetrics.length === 0 ? (
				<Box textAlign="center" p={6}>
					<Text>No active streams found</Text>
				</Box>
			) : (
				<>
					<Flex justify="flex-end" mb={4}>
						<Button
							colorPalette="red"
							size="sm"
							onClick={cancelAllDialog.onOpen}
							loading={isProcessing}
						>
							Cancel All Channels
						</Button>
					</Flex>

					<Flex
						gap={4}
						direction={{ base: "column", md: "row" }}
						h={{ md: "70vh" }}
					>
						<Box
							w={{ base: "100%", md: "40%" }}
							borderWidth="1px"
							borderRadius="md"
							overflow="hidden"
						>
							<Box p={3}>
								<Heading size="sm">Channels ({channelMetrics.length})</Heading>
							</Box>
							<Box p={2} overflowY="auto" maxH={{ md: "calc(70vh - 40px)" }}>
								{channelMetrics.map((channel) => (
									<ChannelCard
										key={channel.id}
										channel={channel}
										isSelected={channel.id === selectedChannelId}
										onClick={() => setSelectedChannelId(channel.id)}
										onCancelChannel={cancelChannelDialog.onOpen}
										onMoveToNextStream={handleMoveToNextStream}
									/>
								))}
							</Box>
						</Box>

						<Box
							w={{ base: "100%", md: "60%" }}
							borderWidth="1px"
							borderRadius="md"
							overflow="hidden"
						>
							{selectedChannel ? (
								<>
									<Box p={3}>
										<Heading size="sm">
											Clients ({selectedChannel.clientStreams.length})
										</Heading>
									</Box>
									<Box overflowY="auto" maxH={{ md: "calc(70vh - 40px)" }}>
										<Box p={3}>
											<Heading size="xs" mb={2}>
												Source Stream
											</Heading>
											<SourceStreamDetails channel={selectedChannel} />
										</Box>
										<Separator />
										<Box p={3}>
											<Heading size="xs" mb={2}>
												Client Streams
											</Heading>
											<ClientStreamDetails
												clientStreams={selectedChannel.clientStreams}
												channelInfo={selectedChannel}
												onCancelClient={(clientId) => {
													setClientToCancel(clientId);
													cancelClientDialog.onOpen();
												}}
											/>
										</Box>
									</Box>
								</>
							) : (
								<Flex justify="center" align="center" h="100%">
									<Text color="gray.500">Select a channel to view details</Text>
								</Flex>
							)}
						</Box>
					</Flex>

					<Dialog.Root
						open={cancelAllDialog.open}
						onOpenChange={(open) => !open && cancelAllDialog.onClose()}
					>
						<Portal>
							<Dialog.Backdrop />
							<Dialog.Positioner>
								<Dialog.Content>
									<Dialog.Header>
										<Dialog.Title>Cancel All Channels</Dialog.Title>
									</Dialog.Header>
									<Dialog.Body>
										Are you sure you want to cancel all active channels? This
										will disconnect all clients.
									</Dialog.Body>
									<Dialog.Footer>
										<Button onClick={cancelAllDialog.onClose}>Cancel</Button>
										<Button
											colorPalette="red"
											onClick={handleCancelAllChannels}
											ml={3}
											loading={isProcessing}
										>
											Confirm
										</Button>
									</Dialog.Footer>
								</Dialog.Content>
							</Dialog.Positioner>
						</Portal>
					</Dialog.Root>

					<Dialog.Root
						open={cancelChannelDialog.open}
						onOpenChange={(open) => !open && cancelChannelDialog.onClose()}
					>
						<Portal>
							<Dialog.Backdrop />
							<Dialog.Positioner>
								<Dialog.Content>
									<Dialog.Header>
										<Dialog.Title>Cancel Channel</Dialog.Title>
									</Dialog.Header>
									<Dialog.Body>
										Are you sure you want to cancel the channel "
										{selectedChannel?.name}"? This will disconnect all clients
										from this channel.
									</Dialog.Body>
									<Dialog.Footer>
										<Button onClick={cancelChannelDialog.onClose}>
											Cancel
										</Button>
										<Button
											colorPalette="red"
											onClick={handleCancelChannel}
											ml={3}
											loading={isProcessing}
										>
											Confirm
										</Button>
									</Dialog.Footer>
								</Dialog.Content>
							</Dialog.Positioner>
						</Portal>
					</Dialog.Root>

					<Dialog.Root
						open={cancelClientDialog.open}
						onOpenChange={(open) => !open && cancelClientDialog.onClose()}
					>
						<Portal>
							<Dialog.Backdrop />
							<Dialog.Positioner>
								<Dialog.Content>
									<Dialog.Header>
										<Dialog.Title>Cancel Client Stream</Dialog.Title>
									</Dialog.Header>
									<Dialog.Body>
										Are you sure you want to cancel this client stream?
									</Dialog.Body>
									<Dialog.Footer>
										<Button onClick={cancelClientDialog.onClose}>Cancel</Button>
										<Button
											colorPalette="red"
											onClick={handleCancelClient}
											ml={3}
											loading={isProcessing}
										>
											Confirm
										</Button>
									</Dialog.Footer>
								</Dialog.Content>
							</Dialog.Positioner>
						</Portal>
					</Dialog.Root>
				</>
			)}
		</Box>
	);
};
