import {
	ActionBarContent,
	ActionBarRoot,
	ActionBarSelectionTrigger,
	ActionBarSeparator,
	Button,
} from "@chakra-ui/react";

interface StreamSelectionActionBarProps {
	selection: string[];
}

export const StreamSelectionActionBar = ({
	selection,
}: StreamSelectionActionBarProps) => {
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
			</ActionBarContent>
		</ActionBarRoot>
	);
};
