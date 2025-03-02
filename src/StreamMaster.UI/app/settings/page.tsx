import { Box, Heading } from "@chakra-ui/react";
import { SettingsEditor } from "../../components/settings-editor/settings-editor";

export default async function SettingsPage() {
	return (
		<Box>
			<Heading>Settings</Heading>
			<SettingsEditor />
		</Box>
	);
}
