import { Box, HStack, IconButton } from "@chakra-ui/react";
import { LuLink2, LuTrash } from "react-icons/lu";
import { Tooltip } from "../ui/tooltip";
import { apiClient } from "../../lib/api";
import type { components } from "../../lib/api.d";
import { useState } from "react";
import { toaster } from "../ui/toaster";
import { ConfirmationDialog } from "../confirmation-dialog/confirmation-dialog";
import { useMutate } from "../../lib/use-api";

interface StreamRowActionsProps {
	stream: components["schemas"]["SMStreamDto"];
}

export const StreamRowActions = ({ stream }: StreamRowActionsProps) => {
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

	const handleDeleteStream = () => {
		return handleAction(
			"delete",
			async () => {
				if (stream.id) {
					await apiClient.DELETE("/api/smstreams/deletesmstream", {
						body: { smStreamId: stream.id },
					});
					await mutate(["/api/smstreams/getpagedsmstreams"]);
				}
			},
			{
				title: "Stream Deleted",
				description: `Stream "${stream.name}" was successfully deleted.`,
			},
			{
				title: "Deletion Failed",
				description: "Failed to delete stream. Please try again.",
			},
		);
	};

	const handleGetStreamLink = () => {
		return handleAction(
			"getLink",
			async () => {
				if (stream.url) {
					navigator.clipboard.writeText(stream.url);
				} else {
					throw new Error(
						`Failed to get stream link. Stream details ${JSON.stringify(stream, null, 2)}`,
						{ cause: "URL not found" },
					);
				}
			},
			{
				title: "Link Copied",
				description: "Stream link copied to clipboard.",
			},
			{
				title: "Failed to Get Link",
				description: "Could not retrieve stream link. Please try again.",
			},
		);
	};

	return (
		<HStack gap={1}>
			<Tooltip content="Delete Stream">
				<Box>
					<ConfirmationDialog
						title="Delete Stream"
						description={`Are you sure you want to delete "${stream.name}"? It can be added from available Streams.`}
						confirmText="Delete"
						cancelText="Cancel"
						onConfirm={handleDeleteStream}
						variant="warning"
						isLoading={isLoading === "delete"}
						trigger={
							<IconButton
								size={"2xs"}
								aria-label="Delete Stream"
								color={"red.500"}
								variant={"outline"}
								loading={false}
								disabled={!stream.needsDelete}
							>
								<LuTrash />
							</IconButton>
						}
					/>
				</Box>
			</Tooltip>

			<Tooltip content="Get Stream Link">
				<IconButton
					size={"2xs"}
					aria-label="Stream Link"
					color={"green.500"}
					variant={"outline"}
					loading={isLoading === "getLink"}
					onClick={handleGetStreamLink}
					disabled={!stream.url}
				>
					<LuLink2 />
				</IconButton>
			</Tooltip>
		</HStack>
	);
};
