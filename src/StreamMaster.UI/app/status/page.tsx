import { Box } from "@chakra-ui/react";
import { StreamingStatus } from "../../components/streams-status/streams-status";

export default async function StatusPage() {
	return (
		<Box>
			<StreamingStatus />
		</Box>
	);
}
