"use client";
import { Box, Heading, SimpleGrid, GridItem } from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { SwitchField, InputField } from "./settings-editor";

export const GeneralSettings = ({
	settings,
	handleChange,
}: {
	settings: components["schemas"]["SettingDto"];
	handleChange: <K extends keyof components["schemas"]["SettingDto"]>(
		key: K,
		value: components["schemas"]["SettingDto"][K],
	) => void;
}) => {
	return (
		<Box borderWidth="1px" borderRadius="md" p={4} boxShadow="sm">
			<Heading as="h3" size="md" mb={4}>
				General Settings
			</Heading>
			<SimpleGrid columns={{ base: 1 }} gap={4}>
				<GridItem>
					<SwitchField
						label="Debug API"
						isChecked={settings.debugAPI}
						onChange={(checked) => handleChange("debugAPI", checked)}
						helperText="Enable detailed API logging for debugging."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="API Key"
						value={settings.apiKey}
						onChange={(value) => handleChange("apiKey", value)}
						helperText="API key used for authentication."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Delete Old STRM Files"
						isChecked={settings.deleteOldSTRMFiles}
						onChange={(checked) => handleChange("deleteOldSTRMFiles", checked)}
						helperText="Automatically delete old STRM files to save space."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="STRM Base URL"
						value={settings.strmBaseURL}
						onChange={(value) => handleChange("strmBaseURL", value)}
						helperText="Base URL used for generating STRM files."
					/>
				</GridItem>
			</SimpleGrid>
		</Box>
	);
};
