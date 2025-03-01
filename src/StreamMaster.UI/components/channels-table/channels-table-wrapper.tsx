import { apiClient } from "../../lib/api";
import { ChannelsTable } from "./channels-table";

interface WrapperProps {
	page: number;
	pageSize: number;
	paginationPrefix: string;
}

export default async function ChannelsTableWrapper({
	page,
	pageSize,
	paginationPrefix,
}: WrapperProps) {
	const pagedChannels = await apiClient.GET(
		"/api/smchannels/getpagedsmchannels",
		{
			params: {
				query: {
					PageNumber: page,
					PageSize: pageSize,
				},
			},
		},
	);

	const channelsData = {
		data: pagedChannels.data?.data || [],
		totalItemCount: pagedChannels.data?.totalItemCount || 0,
		pageNumber: page,
		pageSize: pageSize,
		paginationPrefix,
	};

	return <ChannelsTable initialChannels={channelsData} />;
}
