"use client";

import { Stack, Flex } from "@chakra-ui/react";
import { useState, useEffect } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import type { components } from "../../lib/api.d";
import { ChannelTableHeader } from "./channels-table-header";
import { ChannelTableBody } from "./channels-table-body";
import { ChannelTablePageSize } from "./channels-table-pagination-size";
import { ChannelTablePagination } from "./channels-table-pagination";
import { ChannelSelectionActionBar } from "./channels-table-action-bar";

interface ChannelsTableProps {
	initialChannels: {
		data: components["schemas"]["SMChannelDto"][];
		totalItemCount: number;
		pageNumber: number;
		pageSize: number;
		paginationPrefix: string;
	} | null;
}

export const ChannelsTable = ({ initialChannels }: ChannelsTableProps) => {
	const [selection, setSelection] = useState<number[]>([]);
	const router = useRouter();
	const pathname = usePathname();
	const searchParams = useSearchParams();

	// Get current page from URL or fallback to initialChannels
	const currentPage = initialChannels?.pageNumber || 1;

	// Reset selection when page changes
	// biome-ignore lint/correctness/useExhaustiveDependencies: Want to reset selection when page changes
	useEffect(() => {
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

	const handlePageChange = (pageInfo: { page: number; pageSize: number }) => {
		const params = new URLSearchParams(searchParams);
		params.set(`${paginationPrefix}Page`, pageInfo.page.toString());

		if (pageInfo.pageSize !== pageSize) {
			params.set(`${paginationPrefix}PageSize`, pageInfo.pageSize.toString());
		}

		router.push(`${pathname}?${params.toString()}`);
	};

	return (
		<>
			<Stack gap={4}>
				<ChannelTableHeader
					channels={channels}
					selection={selection}
					setSelection={setSelection}
				/>
				<ChannelTableBody
					channels={channels}
					selection={selection}
					setSelection={setSelection}
				/>

				<Flex justify="space-between" align="center" wrap="wrap" gap={2}>
					<ChannelTablePageSize
						currentPage={currentPage}
						pageSize={pageSize}
						onPageSizeChange={(newSize) => {
							const firstItemIndex = (currentPage - 1) * pageSize;
							const newPage = Math.floor(firstItemIndex / newSize) + 1;
							handlePageChange({ page: newPage, pageSize: newSize });
						}}
					/>

					<ChannelTablePagination
						totalItemCount={totalItemCount}
						pageSize={pageSize}
						currentPage={currentPage}
						onPageChange={handlePageChange}
					/>
				</Flex>
			</Stack>

			<ChannelSelectionActionBar selection={selection} />
		</>
	);
};
