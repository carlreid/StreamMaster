import { Table } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { Checkbox } from "../ui/checkbox";

interface StreamTableHeaderProps {
	streams: components["schemas"]["SMStreamDto"][];
	selection: string[];
	setSelection: React.Dispatch<React.SetStateAction<string[]>>;
}

export const StreamTableHeader = ({
	streams,
	selection,
	setSelection,
}: StreamTableHeaderProps) => {
	const indeterminate =
		selection.length > 0 && selection.length < streams.length;

	return (
		<Table.Header>
			<Table.Row>
				<Table.ColumnHeader w="6">
					<Checkbox
						top="1"
						aria-label="Select all rows"
						checked={indeterminate ? "indeterminate" : selection.length > 0}
						onCheckedChange={(changes) => {
							setSelection(
								changes.checked ? streams.map((stream) => stream.id || "") : [],
							);
						}}
					/>
				</Table.ColumnHeader>
				<Table.ColumnHeader>Logo</Table.ColumnHeader>
				<Table.ColumnHeader>Name</Table.ColumnHeader>
				<Table.ColumnHeader>Membership</Table.ColumnHeader>
				<Table.ColumnHeader>Group</Table.ColumnHeader>
				<Table.ColumnHeader>M3U File</Table.ColumnHeader>
				<Table.ColumnHeader>Actions</Table.ColumnHeader>
			</Table.Row>
		</Table.Header>
	);
};
