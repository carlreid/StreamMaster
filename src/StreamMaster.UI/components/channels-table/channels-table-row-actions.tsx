import { Box, HStack, IconButton } from "@chakra-ui/react";
import { useState } from "react";
import { LuTrash, LuWandSparkles } from "react-icons/lu";
import { apiClient } from "../../lib/api";
import type { components } from "../../lib/api.d";
import { useMutate } from "../../lib/use-api";
import { ConfirmationDialog } from "../confirmation-dialog/confirmation-dialog";
import { toaster } from "../ui/toaster";
import { Tooltip } from "../ui/tooltip";

interface ChannelRowActionsProps {
	channel: components["schemas"]["SMChannelDto"];
}

export const ChannelRowActions = ({ channel }: ChannelRowActionsProps) => {
	const [isLoading, setIsLoading] = useState<string | null>(null);
	const mutate = useMutate();

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

	const handleDeleteChannel = () => {
		return handleAction(
			"delete",
			async () => {
				if (channel.id) {
					await apiClient.DELETE("/api/smchannels/deletesmchannel", {
						body: { smChannelId: channel.id },
					});
					await mutate(["/api/smchannels/getpagedsmchannels"]);
				}
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
