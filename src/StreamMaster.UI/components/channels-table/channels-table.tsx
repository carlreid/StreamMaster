"use client";

import {
	ActionBarContent,
	ActionBarRoot,
	ActionBarSelectionTrigger,
	ActionBarSeparator,
	Button,
	HStack,
	IconButton,
	Stack,
	Table,
	Flex,
	createListCollection,
} from "@chakra-ui/react";
import { useState, useEffect } from "react";
import {
	LuLink2,
	LuPencil,
	LuTrash,
	LuSettings,
	LuWandSparkles,
} from "react-icons/lu";
import { Checkbox } from "../ui/checkbox";
import {
	PaginationItems,
	PaginationNextTrigger,
	PaginationPrevTrigger,
	PaginationRoot,
} from "../ui/pagination";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import type { components } from "../../lib/api.d";
import {
	SelectContent,
	SelectItem,
	SelectLabel,
	SelectRoot,
	SelectTrigger,
	SelectValueText,
} from "../ui/select";
import {
	PopoverBody,
	PopoverContent,
	PopoverRoot,
	PopoverTitle,
	PopoverTrigger,
} from "../ui/popover";
import { NumberInputField, NumberInputRoot } from "../ui/number-input";
import { apiClient } from "../../lib/api";
import { Tooltip } from "../ui/tooltip";

interface ChannelsTableProps {
	initialChannels: {
		data: components["schemas"]["SMChannelDto"][];
		totalItemCount: number;
		pageNumber: number;
		pageSize: number;
		paginationPrefix: string;
	} | null;
}

// Predefined page size options
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100, 250];

// Define the item type for SelectItem
interface PageSizeOption {
	value: string;
	label: string;
	disabled?: boolean;
}

export const ChannelsTable = ({ initialChannels }: ChannelsTableProps) => {
	const [selection, setSelection] = useState<number[]>([]);
	const [customPageSize, setCustomPageSize] = useState<number>(0);
	const [selectedPageSize, setSelectedPageSize] = useState<string[]>([]);
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	// Get current page from URL or fallback to initialChannels
	const currentPage = initialChannels?.pageNumber || 1;

	// Create the initial page size options
	const pageSizeItems: PageSizeOption[] = PAGE_SIZE_OPTIONS.map((size) => ({
		value: String(size),
		label: String(size),
	}));

	// Add current page size if it's not in the predefined options
	if (
		initialChannels?.pageSize &&
		!PAGE_SIZE_OPTIONS.includes(initialChannels.pageSize)
	) {
		pageSizeItems.push({
			value: String(initialChannels.pageSize),
			label: String(initialChannels.pageSize),
		});
	}

	// Add custom option
	pageSizeItems.push({
		value: "custom",
		label: "Custom...",
	});

	// Create the collection
	const pageSizeOptions = createListCollection({
		items: pageSizeItems,
	});

	// Initialize selected page size and custom page size when component mounts
	useEffect(() => {
		if (initialChannels?.pageSize) {
			const pageSizeStr = String(initialChannels.pageSize);
			setSelectedPageSize([pageSizeStr]);

			if (!PAGE_SIZE_OPTIONS.includes(initialChannels.pageSize)) {
				setCustomPageSize(initialChannels.pageSize);
			}
		}
	}, [initialChannels?.pageSize]);

	// biome-ignore lint/correctness/useExhaustiveDependencies: If currentPage changes, we want to reset selection
	useEffect(() => {
		// Reset selection when page changes
		setSelection([]);
	}, [currentPage]);

	if (!initialChannels || !initialChannels.data) {
		return <div>Loading...</div>;
	}

	const {
		data: channels,
		totalItemCount,
		pageSize,
		paginationPrefix,
	} = initialChannels;

	const hasSelection = selection.length > 0;
	const indeterminate = hasSelection && selection.length < channels.length;

	const handlePageChange = (pageInfo: { page: number; pageSize: number }) => {
		const params = new URLSearchParams(searchParams);

		const pageParam = `${paginationPrefix}Page`;
		params.set(pageParam, pageInfo.page.toString());

		const pageSizeParam = `${paginationPrefix}PageSize`;
		if (pageInfo.pageSize !== pageSize) {
			params.set(pageSizeParam, pageInfo.pageSize.toString());
		}

		router.push(`${pathname}?${params.toString()}`);
	};

	const handlePageSizeChange = (newPageSize: number) => {
		// Calculate the first item index of the current page
		const firstItemIndex = (currentPage - 1) * pageSize;

		// Calculate which page this item would be on with the new page size
		const newPage = Math.floor(firstItemIndex / newPageSize) + 1;

		handlePageChange({ page: newPage, pageSize: newPageSize });
	};

	const handleCustomPageSizeChange = (value: string) => {
		setCustomPageSize(Number(value));
	};

	const applyCustomPageSize = () => {
		if (customPageSize >= 1) {
			handlePageSizeChange(customPageSize);
		}
	};

	const handleSelectChange = (value: string[]) => {
		if (value.includes("custom")) {
			// Custom option selected, don't change page size yet
			setSelectedPageSize(["custom"]);
			return;
		}

		if (value.length > 0) {
			const newSize = Number(value[0]);
			setSelectedPageSize(value);
			handlePageSizeChange(newSize);
		}
	};

	const rows = channels.map((channel) => (
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
			</Table.Cell>
		</Table.Row>
	));

	return (
		<>
			<Stack gap={4}>
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
					<Table.Body>{rows}</Table.Body>
				</Table.Root>

				<Flex justify="space-between" align="center" wrap="wrap" gap={2}>
					<HStack gap={2}>
						<SelectRoot
							value={selectedPageSize}
							collection={pageSizeOptions}
							onValueChange={(e) => handleSelectChange(e.value)}
						>
							<SelectTrigger width="100px">
								<SelectValueText />
							</SelectTrigger>
							<SelectContent>
								<SelectLabel>Page Size</SelectLabel>
								{pageSizeItems.map((option) => (
									<SelectItem key={option.value} item={option}>
										{option.label}
									</SelectItem>
								))}
							</SelectContent>
						</SelectRoot>

						<PopoverRoot>
							<PopoverTrigger asChild>
								<IconButton
									aria-label="Custom page size"
									size="sm"
									variant="ghost"
								>
									<LuSettings />
								</IconButton>
							</PopoverTrigger>
							<PopoverContent>
								<PopoverBody>
									<PopoverTitle>Custom Page Size</PopoverTitle>
									<Stack gap={3} py={2}>
										<NumberInputRoot
											value={String(customPageSize)}
											onValueChange={(e) => handleCustomPageSizeChange(e.value)}
										>
											<NumberInputField min={1} max={1000} />
										</NumberInputRoot>
										<Button size="sm" onClick={applyCustomPageSize}>
											Apply
										</Button>
									</Stack>
								</PopoverBody>
							</PopoverContent>
						</PopoverRoot>
					</HStack>

					{/* Pagination Controls */}
					<PaginationRoot
						count={totalItemCount}
						pageSize={pageSize}
						page={currentPage}
						onPageChange={handlePageChange}
					>
						<HStack wrap="wrap" justify="center">
							<PaginationPrevTrigger />
							<PaginationItems />
							<PaginationNextTrigger />
						</HStack>
					</PaginationRoot>
				</Flex>
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
		</>
	);
};
