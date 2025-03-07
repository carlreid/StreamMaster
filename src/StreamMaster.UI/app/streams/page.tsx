import { Box, GridItem, Heading, SimpleGrid } from "@chakra-ui/react";
import { ChannelsTableWrapper } from "../../components/channels-table/channels-table-wrapper";
import { StreamsTableWrapper } from "../../components/streams-table/streams-table-wrapper";
import { QuickAccessUrls } from "../../components/quick-access-urls/quick-access-urls";

interface StreamsPageSearchParams {
	channelsPage?: string;
	channelsPageSize?: string;
	streamsPage?: string;
	streamsPageSize?: string;
}

interface PageProps {
	params: Promise<{ [key: string]: string }>;
	searchParams?: Promise<StreamsPageSearchParams>;
}

export default async function StreamsPage({ searchParams }: PageProps) {
	const waitedSearchParams = await searchParams;

	const channelsPage = Number(waitedSearchParams?.channelsPage) || 1;
	const channelsPageSize = Number(waitedSearchParams?.channelsPageSize) || 25;

	const streamsPage = Number(waitedSearchParams?.streamsPage) || 1;
	const streamsPageSize = Number(waitedSearchParams?.streamsPageSize) || 25;

	return (
		<SimpleGrid columns={2} gap={4}>
			<GridItem>
				<Heading>Channels</Heading>
				<ChannelsTableWrapper
					page={channelsPage}
					pageSize={channelsPageSize}
					paginationPrefix="channels"
				/>
			</GridItem>
			<GridItem>
				<Heading>Streams</Heading>
				<StreamsTableWrapper
					page={streamsPage}
					pageSize={streamsPageSize}
					paginationPrefix="streams"
				/>
			</GridItem>
		</SimpleGrid>
	);
}
