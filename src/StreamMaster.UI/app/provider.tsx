"use client";

import { ChakraProvider } from "@chakra-ui/react";
import { ColorModeProvider } from "../components/ui/color-mode";
import { system } from "../config/theme";

export default function RootLayout(props: { children: React.ReactNode }) {
	return (
		<ChakraProvider value={system}>
			<ColorModeProvider forcedTheme="dark">{props.children}</ColorModeProvider>
		</ChakraProvider>
	);
}
