import {
	ActionBarContent,
	ActionBarRoot,
	ActionBarSelectionTrigger,
	ActionBarSeparator,
	Button,
} from "@chakra-ui/react";
import { apiClient } from "../../lib/api";

interface ChannelSelectionActionBarProps {
	selection: number[];
}

export const ChannelSelectionActionBar = ({
	selection,
}: ChannelSelectionActionBarProps) => {
	const hasSelection = selection.length > 0;

	return (
		<ActionBarRoot open={hasSelection}>
			<ActionBarContent>
				<ActionBarSelectionTrigger>
					{selection.length} selected
				</ActionBarSelectionTrigger>
				<ActionBarSeparator />
				<Button variant="outline" size="sm" color={"red.500"}>
					Delete
				</Button>
				<Button
					variant="outline"
					size="sm"
					color={"blue.500"}
					onClick={() =>
						void apiClient.PATCH("/api/smchannels/autosetepg", {
							body: { ids: selection },
						})
					}
				>
					Auto Set EPG
				</Button>
			</ActionBarContent>
		</ActionBarRoot>
	);
};
