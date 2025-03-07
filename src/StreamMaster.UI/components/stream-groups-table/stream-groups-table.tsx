"use client";

import {
	Box,
	Button,
	Flex,
	Heading,
	IconButton,
	Stack,
	Table,
	Text,
} from "@chakra-ui/react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";

import type { components } from "../../lib/api.d";
import { useApi } from "../../lib/use-api";
import { GenericTablePagination } from "../generic-table/generic-table-pagination";
import { GenericTablePageSize } from "../generic-table/generic-table-pagination-size";
import { toaster } from "../ui/toaster";
import { Tag } from "../ui/tag";
import { Tooltip } from "../ui/tooltip";
import { MenuContent, MenuItem, MenuRoot, MenuTrigger } from "../ui/menu";
import { LuCopy, LuEllipsis, LuExternalLink, LuLink } from "react-icons/lu";

interface StreamGroupsTableProps {
	initialData?: components["schemas"]["PagedResponseOfStreamGroupDto"];
	pageNumber: number;
	pageSize: number;
	paginationPrefix: string;
}

export const StreamGroupsTable = (props: StreamGroupsTableProps) => {
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	const currentPage = props.pageNumber || 1;
	const pageSize = props.pageSize || 10;
	const paginationPrefix = props.paginationPrefix || "";

	const { data: streamGroupsData, error } = useApi(
		"/api/streamgroups/getpagedstreamgroups",
		{
			params: {
				query: {
					PageNumber: currentPage,
					PageSize: pageSize,
				},
			},
		},
		{
			fallbackData: props.initialData,
		},
	);

	if (error) {
		return <Box p={4}>Error loading stream groups</Box>;
	}

	if (!streamGroupsData) {
		return <Box p={4}>Loading...</Box>;
	}

	const { data: streamGroups, totalItemCount } = streamGroupsData;

	const handlePageChange = (pageInfo: { page: number; pageSize: number }) => {
		const params = new URLSearchParams(searchParams);
		params.set(`${paginationPrefix}Page`, pageInfo.page.toString());

		if (pageInfo.pageSize !== pageSize) {
			params.set(`${paginationPrefix}PageSize`, pageInfo.pageSize.toString());
		}

		router.push(`${pathname}?${params.toString()}`);
	};

	const copyToClipboard = (text: string | undefined, label: string) => {
		if (!text) return;

		navigator.clipboard.writeText(text);
		toaster.create({
			title: "Copied to clipboard",
			description: `${label} link has been copied`,
			duration: 2000,
		});
	};

	const openLink = (url: string | undefined) => {
		if (!url) return;
		window.open(url, "_blank");
	};

	return (
		<Stack gap={4}>
			<Box overflowX="auto">
				<Table.Root size="md">
					<Table.Header>
						<Table.Row>
							<Table.ColumnHeader>Group Name</Table.ColumnHeader>
							<Table.ColumnHeader>Total Channels</Table.ColumnHeader>
							<Table.ColumnHeader>Type</Table.ColumnHeader>
							<Table.ColumnHeader>Quick Links</Table.ColumnHeader>
							<Table.ColumnHeader>Extra</Table.ColumnHeader>
						</Table.Row>
					</Table.Header>
					<Table.Body>
						{streamGroups?.map((group) => (
							<Table.Row key={group.groupKey}>
								<Table.Cell>
									<Text fontWeight="medium">{group.name}</Text>
									<Text fontSize="sm" color="gray.600">
										{group.groupKey}
									</Text>
								</Table.Cell>
								<Table.Cell>
									<Tag colorPalette="blue">{group.channelCount} channels</Tag>
								</Table.Cell>
								<Table.Cell>
									{group.isSystem && <Tag colorPalette="purple">System</Tag>}
									{group.isReadOnly && (
										<Tag colorPalette="orange" ml={1}>
											Read Only
										</Tag>
									)}
								</Table.Cell>
								<Table.Cell>
									<Flex gap={2} wrap="wrap">
										{group.m3ULink && (
											<Tooltip content="Copy M3U playlist link">
												<Button
													aria-label="M3U Link"
													size="sm"
													colorPalette="blue"
													variant="outline"
													onClick={() =>
														group.m3ULink &&
														copyToClipboard(group.m3ULink, "M3U")
													}
												>
													<LuLink /> M3U
												</Button>
											</Tooltip>
										)}
										{group.xmlLink && (
											<Tooltip content="Copy EPG/XML guide link">
												<Button
													aria-label="EPG/XML Link"
													size="sm"
													colorPalette="green"
													variant="outline"
													onClick={() =>
														group.xmlLink &&
														copyToClipboard(group.xmlLink, "EPG/XML")
													}
												>
													<LuLink /> EPG
												</Button>
											</Tooltip>
										)}
										{group.hdhrLink && (
											<Tooltip content="Copy HDHR link">
												<Button
													aria-label="HDHR Link"
													size="sm"
													colorPalette="purple"
													variant="outline"
													onClick={() =>
														group.hdhrLink &&
														copyToClipboard(group.hdhrLink, "HDHR")
													}
												>
													<LuLink /> HDHR
												</Button>
											</Tooltip>
										)}
									</Flex>
								</Table.Cell>

								<Table.Cell>
									<MenuRoot>
										<MenuTrigger asChild>
											<IconButton
												aria-label="Options"
												variant="ghost"
												size="sm"
											>
												<LuEllipsis />
											</IconButton>
										</MenuTrigger>
										<MenuContent>
											<MenuItem
												value="copy-m3u"
												disabled={!group.m3ULink}
												onClick={() => copyToClipboard(group.m3ULink, "M3U")}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy M3U Link
											</MenuItem>
											<MenuItem
												value="copy-short-m3u"
												disabled={!group.shortM3ULink}
												onClick={() =>
													copyToClipboard(group.shortM3ULink, "Short M3U")
												}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy Short M3U
												Link
											</MenuItem>
											<MenuItem
												value="copy-xml"
												disabled={!group.xmlLink}
												onClick={() =>
													copyToClipboard(group.xmlLink, "EPG/XML")
												}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy EPG/XML
												Link
											</MenuItem>
											<MenuItem
												value="copy-short-epg"
												disabled={!group.shortEPGLink}
												onClick={() =>
													copyToClipboard(group.shortEPGLink, "Short EPG")
												}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy Short EPG
												Link
											</MenuItem>
											<MenuItem
												value="copy-hdhr"
												disabled={!group.hdhrLink}
												onClick={() => copyToClipboard(group.hdhrLink, "HDHR")}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy HDHR Link
											</MenuItem>
											<MenuItem
												value="copy-short-hdhr"
												disabled={!group.shortHDHRLink}
												onClick={() =>
													copyToClipboard(group.shortHDHRLink, "Short HDHR")
												}
											>
												<LuCopy style={{ marginRight: "8px" }} /> Copy Short
												HDHR Link
											</MenuItem>
											<MenuItem
												value="open-m3u"
												disabled={!group.m3ULink}
												onClick={() => openLink(group.m3ULink)}
											>
												<LuExternalLink style={{ marginRight: "8px" }} /> Open
												M3U Link
											</MenuItem>
										</MenuContent>
									</MenuRoot>
								</Table.Cell>
							</Table.Row>
						))}
					</Table.Body>
				</Table.Root>
			</Box>

			{streamGroups && streamGroups.length === 0 && (
				<Box textAlign="center" py={8}>
					<Text>No stream groups found</Text>
				</Box>
			)}

			<Flex justify="space-between" align="center" wrap="wrap" gap={2}>
				<GenericTablePageSize
					currentPage={currentPage}
					pageSize={pageSize}
					onPageSizeChange={(newSize) => {
						const firstItemIndex = (currentPage - 1) * pageSize;
						const newPage = Math.floor(firstItemIndex / newSize) + 1;
						handlePageChange({ page: newPage, pageSize: newSize });
					}}
				/>

				<GenericTablePagination
					totalItemCount={totalItemCount}
					pageSize={pageSize}
					currentPage={currentPage}
					onPageChange={handlePageChange}
				/>
			</Flex>
		</Stack>
	);
};
