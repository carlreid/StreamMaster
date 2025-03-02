"use client";
import {
	Box,
	Heading,
	SimpleGrid,
	GridItem,
	createListCollection,
} from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { InputField } from "./settings-editor";
import {
	SelectContent,
	SelectItem,
	SelectLabel,
	SelectRoot,
	SelectTrigger,
	SelectValueText,
} from "../ui/select";

const authMethodOptions = createListCollection({
	items: [
		{ value: "None", label: "None" },
		{ value: "Forms", label: "Forms" },
	],
});

export const AuthenticationSettings = ({
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
				Authentication Settings
			</Heading>
			<SimpleGrid columns={{ base: 1 }} gap={4}>
				<GridItem>
					<InputField
						label="Admin Username"
						value={settings.adminUserName}
						onChange={(value) => handleChange("adminUserName", value)}
						helperText="Username for admin access."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Admin Password"
						type="password"
						value={settings.adminPassword}
						onChange={(value) => handleChange("adminPassword", value)}
						helperText="Password for admin access."
					/>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"authenticationMethod",
								value[0] as components["schemas"]["SettingDto"]["authenticationMethod"],
							)
						}
						value={[settings.authenticationMethod || "None"]}
						collection={authMethodOptions}
					>
						<SelectLabel>Authentication Method</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Authentication Method" />
						</SelectTrigger>
						<SelectContent>
							{authMethodOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						None disables authentication, Forms uses a username and password
						login.
					</Box>
				</GridItem>
			</SimpleGrid>
		</Box>
	);
};
