"use client";

import { ChakraProvider } from "@chakra-ui/react";
import { ColorModeProvider } from "../components/ui/color-mode";
import { system } from "../config/theme";
import { LogoDialogProvider } from "../components/logo-selector-dialog/logo-selector-dialog-context";
import { SignalRProvider } from "../providers/SignalRProvider";

export default function RootLayout(props: { children: React.ReactNode }) {
	return (
		<SignalRProvider>
			<ChakraProvider value={system}>
				<ColorModeProvider>
					<LogoDialogProvider>{props.children}</LogoDialogProvider>
				</ColorModeProvider>
			</ChakraProvider>
		</SignalRProvider>
	);
}
