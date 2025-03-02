import { Box, IconButton, Image, Table } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { Checkbox } from "../ui/checkbox";
import { StreamRowActions } from "./streams-table-row-actions";

interface StreamTableBodyProps {
	streams: components["schemas"]["SMStreamDto"][];
	selection: string[];
	setSelection: React.Dispatch<React.SetStateAction<string[]>>;
}

export const StreamTableBody = ({
	streams,
	selection,
	setSelection,
}: StreamTableBodyProps) => {
	return (
		<Table.Body>
			{streams.map((stream) => (
				<Table.Row
					key={stream.id}
					data-selected={selection.includes(stream.id || "")}
				>
					<Table.Cell>
						<Checkbox
							top="1"
							aria-label="Select row"
							checked={selection.includes(stream.id || "")}
							onCheckedChange={(changes) => {
								setSelection((prev) =>
									changes.checked
										? [...prev, stream.id || ""]
										: selection.filter((id) => id !== stream.id),
								);
							}}
						/>
					</Table.Cell>
					<Table.Cell>
						<IconButton
							position="relative"
							overflow="hidden"
							size="2xl"
							_before={{
								content: '""',
								position: "absolute",
								top: 0,
								left: 0,
								right: 0,
								bottom: 0,
								background: "linear-gradient(135deg, #F7FAFC 25%, #2D3748 75%)",
								animation: "pulse 3s ease-in-out infinite alternate",
								zIndex: -1,
							}}
						>
							<Image
								src={stream.logo}
								alt={stream.name}
								fit="contain"
								aspectRatio={1}
								height="100%"
								width="auto"
							/>
						</IconButton>
					</Table.Cell>
					<Table.Cell wordBreak="break-word" overflow="hidden" maxWidth="60">
						<Box
							textOverflow={"ellipsis"}
							width={"100%"}
							overflow={"hidden"}
							whiteSpace={"nowrap"}
						>
							{stream.name}
						</Box>
					</Table.Cell>
					<Table.Cell>
						{stream.channelMembership?.map((m) => m.name).join(", ")}
					</Table.Cell>
					<Table.Cell>{stream.group}</Table.Cell>
					<Table.Cell>{stream.m3UFileName}</Table.Cell>
					<Table.Cell>
						<StreamRowActions stream={stream} />
					</Table.Cell>
				</Table.Row>
			))}
		</Table.Body>
	);
};
