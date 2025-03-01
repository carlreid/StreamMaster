import { Table } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { Checkbox } from "../ui/checkbox";

interface ChannelTableHeaderProps {
	channels: components["schemas"]["SMChannelDto"][];
	selection: number[];
	setSelection: React.Dispatch<React.SetStateAction<number[]>>;
}

export const ChannelTableHeader = ({
	channels,
	selection,
	setSelection,
}: ChannelTableHeaderProps) => {
	const indeterminate =
		selection.length > 0 && selection.length < channels.length;

	return (
		<Table.Root interactive stickyHeader size={"sm"}>
			<Table.Header>
				<Table.Row>
					<Table.ColumnHeader w="6">
						<Checkbox
							top="1"
							aria-label="Select all rows"
							checked={indeterminate ? "indeterminate" : selection.length > 0}
							onCheckedChange={(changes) => {
								setSelection(
									changes.checked
										? channels.map((channel) => channel.id || -1)
										: [],
								);
							}}
						/>
					</Table.ColumnHeader>
					<Table.ColumnHeader>Number</Table.ColumnHeader>
					<Table.ColumnHeader>Logo</Table.ColumnHeader>
					<Table.ColumnHeader>Name</Table.ColumnHeader>
					<Table.ColumnHeader>EPG</Table.ColumnHeader>
					<Table.ColumnHeader>Group</Table.ColumnHeader>
					<Table.ColumnHeader>Actions</Table.ColumnHeader>
				</Table.Row>
			</Table.Header>
		</Table.Root>
	);
};
