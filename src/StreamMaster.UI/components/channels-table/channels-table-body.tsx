import { IconButton, Table, Image } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { Checkbox } from "../ui/checkbox";
import { ChannelRowActions } from "./channels-table-row-actions";
import { useLogoDialog } from "../logo-selector-dialog/logo-selector-dialog-context";

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
	const { openLogoDialog } = useLogoDialog();

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
					<Table.Cell textAlign={"center"}>
						{channel.logo && (
							<IconButton
								onClick={() => channel && openLogoDialog(channel)}
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
									background:
										"linear-gradient(135deg, #F7FAFC 25%, #2D3748 75%)",
									animation: "pulse 3s ease-in-out infinite alternate",
									zIndex: -1,
								}}
							>
								<Image
									src={channel.logo}
									alt={channel.name}
									fit="contain"
									aspectRatio={1}
									height="100%"
									width="auto"
								/>
							</IconButton>
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
