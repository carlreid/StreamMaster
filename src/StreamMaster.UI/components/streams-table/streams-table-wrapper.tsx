import { apiClient } from "../../lib/api";
import { StreamsTable } from "./streams-table";

interface WrapperProps {
	page: number;
	pageSize: number;
	paginationPrefix: string;
}

export async function StreamsTableWrapper({
	page,
	pageSize,
	paginationPrefix,
}: WrapperProps) {
	const pagedChannels = await apiClient.GET(
		"/api/smstreams/getpagedsmstreams",
		{
			params: {
				query: {
					PageNumber: page,
					PageSize: pageSize,
				},
			},
		},
	);

	return (
		<StreamsTable
			initialData={pagedChannels.data}
			pageNumber={page}
			pageSize={pageSize}
			paginationPrefix={paginationPrefix}
		/>
	);
}
