import { Box, Dialog, HStack, IconButton } from "@chakra-ui/react";
import { LuLink2, LuPencil, LuTrash, LuWandSparkles } from "react-icons/lu";
import { Tooltip } from "../ui/tooltip";
import { apiClient } from "../../lib/api";
import type { components } from "../../lib/api.d";
import { useState } from "react";
import { toaster } from "../ui/toaster";
import { ConfirmationDialog } from "../confirmation-dialog/confirmation-dialog";

interface ChannelRowActionsProps {
	channel: components["schemas"]["SMChannelDto"];
}

export const ChannelRowActions = ({ channel }: ChannelRowActionsProps) => {
	const [isLoading, setIsLoading] = useState<string | null>(null);

	const handleAction = async (
		actionType: string,
		// biome-ignore lint/suspicious/noExplicitAny: Do not care for response type
		actionFn: () => Promise<any>,
		successMessage: { title: string; description: string },
		errorMessage: { title: string; description: string },
	) => {
		setIsLoading(actionType);

		try {
			await actionFn();
			toaster.create({
				title: successMessage.title,
				description: successMessage.description,
				type: "success",
			});
		} catch (error) {
			console.error(`Error during ${actionType}:`, error);
			toaster.create({
				title: errorMessage.title,
				description: errorMessage.description,
				type: "error",
			});
		} finally {
			setIsLoading(null);
		}
	};

	const handleEditChannel = () => {
		return handleAction(
			"edit",
			async () => {
				// Replace with actual edit implementation
				await new Promise((resolve) => setTimeout(resolve, 500));
			},
			{
				title: "Channel Updated",
				description: `Channel "${channel.name}" was successfully updated.`,
			},
			{
				title: "Update Failed",
				description: "Failed to update channel. Please try again.",
			},
		);
	};

	const handleDeleteChannel = () => {
		return handleAction(
			"delete",
			async () => {
				channel.id &&
					apiClient.DELETE("/api/smchannels/deletesmchannel", {
						body: { smChannelId: channel.id },
					});
				// Replace with actual delete implementation
				await new Promise((resolve) => setTimeout(resolve, 500));
			},
			{
				title: "Channel Deleted",
				description: `Channel "${channel.name}" was successfully deleted.`,
			},
			{
				title: "Deletion Failed",
				description: "Failed to delete channel. Please try again.",
			},
		);
	};

	const handleGetChannelLink = () => {
		return handleAction(
			"getLink",
			async () => {
				// Replace with actual get link implementation
				await new Promise((resolve) => setTimeout(resolve, 500));
				// Example: Copy to clipboard
				navigator.clipboard.writeText(
					`https://example.com/channel/${channel.id}`,
				);
			},
			{
				title: "Link Copied",
				description: "Channel link copied to clipboard.",
			},
			{
				title: "Failed to Get Link",
				description: "Could not retrieve channel link. Please try again.",
			},
		);
	};

	const handleAutoSetEpg = () => {
		if (!channel.id) return;

		return handleAction(
			"autoSetEpg",
			async () => {
				await apiClient.PATCH("/api/smchannels/autosetepg", {
					// biome-ignore lint/style/noNonNullAssertion: Channel ID is guaranteed to be non-null, it is checked earlier in code flow
					body: { ids: [channel.id!] },
				});
			},
			{
				title: "EPG Updated",
				description: `EPG for "${channel.name}" was automatically set.`,
			},
			{
				title: "EPG Update Failed",
				description: "Failed to automatically set EPG. Please try again.",
			},
		);
	};

	return (
		<HStack gap={1}>
			<Tooltip content="Edit Channel">
				<IconButton
					size={"2xs"}
					aria-label="Edit Channel"
					color={"blue.500"}
					variant={"outline"}
					loading={isLoading === "edit"}
					onClick={handleEditChannel}
				>
					<LuPencil />
				</IconButton>
			</Tooltip>

			<Tooltip content="Delete Channel">
				<Box>
					<ConfirmationDialog
						title="Delete Channel"
						description={`Are you sure you want to delete "${channel.name}"? It can be added from available Streams.`}
						confirmText="Delete"
						cancelText="Cancel"
						onConfirm={handleDeleteChannel}
						variant="warning"
						isLoading={isLoading === "delete"}
						trigger={
							<IconButton
								size={"2xs"}
								aria-label="Delete Channel"
								color={"red.500"}
								variant={"outline"}
								loading={false}
							>
								<LuTrash />
							</IconButton>
						}
					/>
				</Box>
			</Tooltip>

			<Tooltip content="Get Channel Link">
				<IconButton
					size={"2xs"}
					aria-label="Channel Link"
					color={"green.500"}
					variant={"outline"}
					loading={isLoading === "getLink"}
					onClick={handleGetChannelLink}
				>
					<LuLink2 />
				</IconButton>
			</Tooltip>
			<Tooltip content="Auto Set EPG">
				<IconButton
					size={"2xs"}
					aria-label="Auto Set EPG"
					color={"orange.500"}
					variant={"outline"}
					loading={isLoading === "autoSetEpg"}
					onClick={handleAutoSetEpg}
				>
					<LuWandSparkles />
				</IconButton>
			</Tooltip>
		</HStack>
	);
};
