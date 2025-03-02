import { Table } from "@chakra-ui/react";
import { apiClient } from "../../lib/api";
import type { components } from "../../lib/api.d";
import { useMutate } from "../../lib/use-api";
import { LazyLogoSelectorDialog } from "../logo-selector-dialog/lazy-logo-selector-dialog";
import { Checkbox } from "../ui/checkbox";
import { toaster } from "../ui/toaster";
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
	const mutate = useMutate();
	const handleLogoSelect = async (
		channelId: number,
		logo: string,
	): Promise<{ success: boolean; message: string }> => {
		try {
			await apiClient.PATCH("/api/smchannels/setsmchannellogo", {
				body: {
					smChannelId: channelId,
					logo: logo,
				},
			});
			await mutate(["/api/smchannels/getpagedsmchannels"]);

			toaster.create({
				title: "Logo updated",
				description: "Logo has been updated successfully",
				type: "success",
			});
			return { success: true, message: "Logo updated" };
		} catch (e) {
			console.error(e);
			return { success: false, message: "Error updating logo" };
		}
	};

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
						<LazyLogoSelectorDialog
							onSelectLogo={async (logo) => {
								if (!channel.id) {
									return {
										success: false,
										message: "Can't update logo for channel without ID",
									};
								}
								return await handleLogoSelect(channel.id, logo.url);
							}}
							currentLogoUrl={channel.logo}
							currentLogoDisplayName={channel.name}
							trigger={
								channel.logo && (
									<img
										src={channel.logo}
										alt={`${channel.name} logo`}
										style={{ height: "24px", width: "auto" }}
									/>
								)
							}
						/>
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
