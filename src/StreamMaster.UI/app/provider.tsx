"use client";

import { ChakraProvider } from "@chakra-ui/react";
import { ColorModeProvider } from "../components/ui/color-mode";
import { system } from "../config/theme";
import { LogoDialogProvider } from "../components/logo-selector-dialog/logo-selector-dialog-context";

export default function RootLayout(props: { children: React.ReactNode }) {
	return (
		<ChakraProvider value={system}>
			<ColorModeProvider>
				<LogoDialogProvider>{props.children}</LogoDialogProvider>
			</ColorModeProvider>
		</ChakraProvider>
	);
}
