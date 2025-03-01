import { HStack } from "@chakra-ui/react";
import {
	PaginationItems,
	PaginationNextTrigger,
	PaginationPrevTrigger,
	PaginationRoot,
} from "../ui/pagination";

interface ChannelTablePaginationProps {
	totalItemCount: number;
	pageSize: number;
	currentPage: number;
	onPageChange: (pageInfo: { page: number; pageSize: number }) => void;
}

export const ChannelTablePagination = ({
	totalItemCount,
	pageSize,
	currentPage,
	onPageChange,
}: ChannelTablePaginationProps) => {
	return (
		<PaginationRoot
			count={totalItemCount}
			pageSize={pageSize}
			page={currentPage}
			onPageChange={onPageChange}
		>
			<HStack wrap="wrap" justify="center">
				<PaginationPrevTrigger />
				<PaginationItems />
				<PaginationNextTrigger />
			</HStack>
		</PaginationRoot>
	);
};
