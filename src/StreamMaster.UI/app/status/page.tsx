import { Box } from "@chakra-ui/react";
import { StreamGroupsTable } from "../../components/stream-groups-table/stream-groups-table";

export default async function StatusPage() {
	return (
		<Box textAlign="center" fontSize="xl" pt="30vh">
			Status
			<StreamGroupsTable
				pageNumber={0}
				pageSize={0}
				paginationPrefix={"sgtable"}
			/>
		</Box>
	);
}
