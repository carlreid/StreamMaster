import { HStack, IconButton } from "@chakra-ui/react";
import { LuLink2, LuPencil, LuTrash, LuWandSparkles } from "react-icons/lu";
import { Tooltip } from "../ui/tooltip";
import { apiClient } from "../../lib/api";
import type { components } from "../../lib/api.d";

interface ChannelRowActionsProps {
	channel: components["schemas"]["SMChannelDto"];
}

export const ChannelRowActions = ({ channel }: ChannelRowActionsProps) => {
	return (
		<HStack gap={1}>
			<Tooltip content="Edit Channel">
				<IconButton
					size={"2xs"}
					aria-label="Edit Channel"
					color={"blue.500"}
					variant={"outline"}
				>
					<LuPencil />
				</IconButton>
			</Tooltip>
			<Tooltip content="Delete Channel">
				<IconButton
					size={"2xs"}
					aria-label="Delete Channel"
					color={"red.500"}
					variant={"outline"}
				>
					<LuTrash />
				</IconButton>
			</Tooltip>
			<Tooltip content="Get Channel Link">
				<IconButton
					size={"2xs"}
					aria-label="Channel Link"
					color={"green.500"}
					variant={"outline"}
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
					onClick={() =>
						channel.id &&
						void apiClient.PATCH("/api/smchannels/autosetepg", {
							body: { ids: [channel.id] },
						})
					}
				>
					<LuWandSparkles />
				</IconButton>
			</Tooltip>
		</HStack>
	);
};
