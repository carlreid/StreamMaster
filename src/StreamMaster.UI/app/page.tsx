import { Box, Heading, Text, VStack } from "@chakra-ui/react";
import { StreamGroupsTable } from "../components/stream-groups-table/stream-groups-table";

export default async function Page() {
	return (
		<Box textAlign="center" fontSize="xl" pt="30vh">
			<VStack gap="8">
				<Heading size="2xl" letterSpacing="tight">
					Welcome to StreamMaster UI v2
				</Heading>

				<Text color="gray.500" maxW="600px" px="4">
					This is an early development preview with limited features. We're
					actively working on improvements and additional functionality. Please
					expect changes and potential issues as we refine the experience.
				</Text>

				<StreamGroupsTable pageNumber={0} pageSize={0} paginationPrefix={""} />
			</VStack>
		</Box>
	);
}
