import {
	Box,
	Button,
	Checkbox,
	ClientOnly,
	HStack,
	Heading,
	Progress,
	RadioGroup,
	SimpleGrid,
	Skeleton,
	VStack,
} from "@chakra-ui/react";
import Image from "next/image";
import { ChannelsTable } from "../../components/channels-table";

export default async function StreamsPage() {
	return (
		<SimpleGrid columns={2} gap={4}>
			<Box>
				<Heading>Channels</Heading>
				<ChannelsTable />
			</Box>
			<Box>
				<Heading>Streams</Heading>
				<ChannelsTable />
			</Box>
		</SimpleGrid>
	);
}
