import { Box, Heading, SimpleGrid } from "@chakra-ui/react";
import ChannelsTableWrapper from "../../components/channels-table/channels-table-wrapper";

interface StreamsPageSearchParams {
	channelsPage?: string;
	channelsPageSize?: string;
	streamsPage?: string;
	streamsPageSize?: string;
}

interface PageProps {
	params: { [key: string]: string };
	searchParams?: StreamsPageSearchParams;
}

export default async function StreamsPage({ searchParams }: PageProps) {
	const waitedSearchParams = await searchParams;

	const channelsPage = Number(waitedSearchParams?.channelsPage) || 1;
	const channelsPageSize = Number(waitedSearchParams?.channelsPageSize) || 10;

	const streamsPage = Number(waitedSearchParams?.streamsPage) || 1;
	const streamsPageSize = Number(waitedSearchParams?.streamsPageSize) || 10;

	return (
		<SimpleGrid columns={2} gap={4}>
			<Box>
				<Heading>Channels</Heading>
				<ChannelsTableWrapper
					page={channelsPage}
					pageSize={channelsPageSize}
					paginationPrefix="channels"
				/>
			</Box>
			<Box>
				<Heading>Streams</Heading>
				<ChannelsTableWrapper
					page={streamsPage}
					pageSize={streamsPageSize}
					paginationPrefix="streams"
				/>
			</Box>
		</SimpleGrid>
	);
}
