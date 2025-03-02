"use client";

import { Flex, Stack, Table } from "@chakra-ui/react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";
import type { components } from "../../lib/api.d";
import { useApi } from "../../lib/use-api";
import { GenericTablePagination } from "../generic-table/generic-table-pagination";
import { GenericTablePageSize } from "../generic-table/generic-table-pagination-size";
import { ChannelSelectionActionBar } from "./channels-table-action-bar";
import { ChannelTableBody } from "./channels-table-body";
import { ChannelTableHeader } from "./channels-table-header";

interface ChannelsTableProps {
	initialData?: components["schemas"]["PagedResponseOfSMChannelDto"];
	pageNumber: number;
	pageSize: number;
	paginationPrefix: string;
}

export const ChannelsTable = (props: ChannelsTableProps) => {
	const [selection, setSelection] = useState<number[]>([]);
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	const currentPage = props.pageNumber || 1;
	const pageSize = props.pageSize || 10;
	const paginationPrefix = props.paginationPrefix || "";

	const { data: channelsData, error } = useApi(
		"/api/smchannels/getpagedsmchannels",
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

	// Reset selection when page changes
	// biome-ignore lint/correctness/useExhaustiveDependencies: Want to reset selection when page changes
	useEffect(() => {
		setSelection([]);
	}, [currentPage]);

	if (error) {
		return <div>Error loading channels</div>;
	}

	if (!channelsData) {
		return <div>Loading...</div>;
	}

	const { data: channels, totalItemCount } = channelsData;

	const handlePageChange = (pageInfo: { page: number; pageSize: number }) => {
		const params = new URLSearchParams(searchParams);
		params.set(`${paginationPrefix}Page`, pageInfo.page.toString());

		if (pageInfo.pageSize !== pageSize) {
			params.set(`${paginationPrefix}PageSize`, pageInfo.pageSize.toString());
		}

		router.push(`${pathname}?${params.toString()}`);
	};

	return (
		<Stack>
			<Table.Root interactive stickyHeader showColumnBorder size={"sm"}>
				<ChannelTableHeader
					channels={channels || []}
					selection={selection}
					setSelection={setSelection}
				/>
				<ChannelTableBody
					channels={channels || []}
					selection={selection}
					setSelection={setSelection}
				/>
			</Table.Root>
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

			<ChannelSelectionActionBar selection={selection} />
		</Stack>
	);
};
