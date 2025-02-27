"use client";

import {
	ActionBarContent,
	ActionBarRoot,
	ActionBarSelectionTrigger,
	ActionBarSeparator,
	Button,
	HStack,
	IconButton,
	PaginationNextTrigger,
	PaginationPrevTrigger,
	PaginationRoot,
	Stack,
	Table,
} from "@chakra-ui/react";
import { useState } from "react";
import { Checkbox } from "./ui/checkbox";
import { LuLink2, LuPencil, LuTrash } from "react-icons/lu";

export const ChannelsTable = () => {
	const [selection, setSelection] = useState<string[]>([]);

	const hasSelection = selection.length > 0;
	const indeterminate = hasSelection && selection.length < items.length;

	const rows = items.map((item) => (
		<Table.Row
			key={item.name}
			data-selected={selection.includes(item.name) ? "" : undefined}
		>
			<Table.Cell>
				<Checkbox
					top="1"
					aria-label="Select row"
					checked={selection.includes(item.name)}
					onCheckedChange={(changes) => {
						setSelection((prev) =>
							changes.checked
								? [...prev, item.name]
								: selection.filter((name) => name !== item.name),
						);
					}}
				/>
			</Table.Cell>
			<Table.Cell>{item.id}</Table.Cell>
			<Table.Cell />
			<Table.Cell>{item.name}</Table.Cell>
			<Table.Cell>{item.epg}</Table.Cell>
			<Table.Cell>{item.group}</Table.Cell>
			<Table.Cell>
				<HStack gap={1}>
					<IconButton
						size={"2xs"}
						aria-label="Edit Channel"
						color={"blue.500"}
						variant={"outline"}
					>
						<LuPencil />
					</IconButton>
					<IconButton
						size={"2xs"}
						aria-label="Delete Channel"
						color={"red.500"}
						variant={"outline"}
					>
						<LuTrash />
					</IconButton>
					<IconButton
						size={"2xs"}
						aria-label="Channel Link"
						color={"green.500"}
						variant={"outline"}
					>
						<LuLink2 />
					</IconButton>
				</HStack>
			</Table.Cell>
		</Table.Row>
	));

	return (
		<>
			<Stack>
				<Table.Root interactive stickyHeader size={"sm"}>
					<Table.Header>
						<Table.Row>
							<Table.ColumnHeader w="6">
								<Checkbox
									top="1"
									aria-label="Select all rows"
									checked={
										indeterminate ? "indeterminate" : selection.length > 0
									}
									onCheckedChange={(changes) => {
										setSelection(
											changes.checked ? items.map((item) => item.name) : [],
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
					<Table.Body>{rows}</Table.Body>
				</Table.Root>
			</Stack>
			<ActionBarRoot open={hasSelection}>
				<ActionBarContent>
					<ActionBarSelectionTrigger>
						{selection.length} selected
					</ActionBarSelectionTrigger>
					<ActionBarSeparator />
					<Button variant="outline" size="sm" color={"red.500"}>
						Delete
					</Button>
					<Button variant="outline" size="sm" color={"blue.500"}>
						Auto Set EPG
					</Button>
				</ActionBarContent>
			</ActionBarRoot>
		</>
	);
};
const items = [
	{
		id: 101,
		name: "Stellar Sports 4K",
		epg: "[Stellar Sports]",
		group: "PREMIUM HD/4K",
	},
	{
		id: 102,
		name: "Galaxy Movies",
		epg: "[Galaxy Movies]",
		group: "PREMIUM HD/4K",
	},
	{
		id: 103,
		name: "Horizon News 4K",
		epg: "[Horizon News]",
		group: "NEWS HD/4K",
	},
	{
		id: 104,
		name: "Nebula Documentaries HD",
		epg: "[Nebula Docs]",
		group: "DOCUMENTARY HD/4K",
	},
	{
		id: 105,
		name: "Quantum Entertainment 4K",
		epg: "[Quantum TV]",
		group: "ENTERTAINMENT HD/4K",
	},
	{
		id: 106,
		name: "Fusion Kids *MULTI*",
		epg: "[Fusion Kids *MULTI*]",
		group: "KIDS HD/4K",
	},
	{
		id: 107,
		name: "Apex Nature 4K",
		epg: "[Apex Nature]",
		group: "DOCUMENTARY HD/4K",
	},
	{
		id: 108,
		name: "Pulse Music HD",
		epg: "[Pulse Music]",
		group: "MUSIC HD/4K",
	},
	{
		id: 109,
		name: "Vortex Action 4K RAW",
		epg: "[Vortex Action]",
		group: "MOVIES HD/4K RAW",
	},
	{
		id: 110,
		name: "Zenith Comedy 4K",
		epg: "[Zenith Comedy]",
		group: "ENTERTAINMENT HD/4K",
	},
];
