"use client";

import { Stack, Flex, Table } from "@chakra-ui/react";
import { useState, useEffect } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import type { components } from "../../lib/api.d";
import { StreamTableHeader } from "./streams-table-header";
import { StreamTableBody } from "./streams-table-body";
import { GenericTablePageSize } from "../generic-table/generic-table-pagination-size";
import { GenericTablePagination } from "../generic-table/generic-table-pagination";
import { StreamSelectionActionBar } from "./streams-table-action-bar";
import { useApi } from "../../lib/use-api";

interface StreamsTableProps {
	initialData?: components["schemas"]["PagedResponseOfSMStreamDto"];
	pageNumber: number;
	pageSize: number;
	paginationPrefix: string;
}

export const StreamsTable = (props: StreamsTableProps) => {
	const [selection, setSelection] = useState<string[]>([]);
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	const currentPage = props.pageNumber || 1;
	const pageSize = props.pageSize || 10;
	const paginationPrefix = props.paginationPrefix || "";

	const { data: channelsData, error } = useApi(
		"/api/smstreams/getpagedsmstreams",
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
			<Table.Root interactive stickyHeader size={"sm"}>
				<StreamTableHeader
					streams={channels || []}
					selection={selection}
					setSelection={setSelection}
				/>
				<StreamTableBody
					streams={channels || []}
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

			<StreamSelectionActionBar selection={selection} />
		</Stack>
	);
};
