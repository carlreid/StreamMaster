import { Table } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { Checkbox } from "../ui/checkbox";
import { ChannelRowActions } from "./channels-table-row-actions";

interface ChannelTableBodyProps {
	channels: components["schemas"]["SMChannelDto"][];
	selection: number[];
	setSelection: React.Dispatch<React.SetStateAction<number[]>>;
}

export const ChannelTableBody = ({
	channels,
	selection,
	setSelection,
}: ChannelTableBodyProps) => {
	return (
		<Table.Body>
			{channels.map((channel) => (
				<Table.Row
					key={channel.id}
					data-selected={selection.includes(channel.id || -1)}
				>
					<Table.Cell>
						<Checkbox
							top="1"
							aria-label="Select row"
							checked={selection.includes(channel.id || -1)}
							onCheckedChange={(changes) => {
								setSelection((prev) =>
									changes.checked
										? [...prev, channel.id || -1]
										: selection.filter((id) => id !== channel.id),
								);
							}}
						/>
					</Table.Cell>
					<Table.Cell>{channel.id}</Table.Cell>
					<Table.Cell>
						{channel.logo && (
							<img
								src={channel.logo}
								alt={`${channel.name} logo`}
								style={{ height: "24px", width: "auto" }}
							/>
						)}
					</Table.Cell>
					<Table.Cell>{channel.name}</Table.Cell>
					<Table.Cell>{channel.epgId}</Table.Cell>
					<Table.Cell>{channel.groupTitle}</Table.Cell>
					<Table.Cell>
						<ChannelRowActions channel={channel} />
					</Table.Cell>
				</Table.Row>
			))}
		</Table.Body>
	);
};
