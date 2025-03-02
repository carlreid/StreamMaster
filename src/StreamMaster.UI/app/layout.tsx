import { Box, Flex } from "@chakra-ui/react";
import type { Metadata } from "next";
import { Inter } from "next/font/google";
import Sidebar from "../components/sidebar";
import { ColorModeButton } from "../components/ui/color-mode";
import { Toaster } from "../components/ui/toaster";
import Provider from "./provider";
import { LogoSelectorDialog } from "../components/logo-selector-dialog/logo-selector-dialog";

export const metadata: Metadata = {
	title: "Stream Master (v2)",
	description: "Experimental version of the Stream Master dashboard.",
};

const inter = Inter({
	subsets: ["latin"],
	display: "swap",
});

export default function RootLayout({
	children,
}: {
	children: React.ReactNode;
}) {
	return (
		<html suppressHydrationWarning className={inter.className} lang="en">
			<head />
			<body>
				<Provider>
					<LogoSelectorDialog />
					<Flex h="100vh" w="100%">
						<Box pos="absolute" top="4" right="4">
							<ColorModeButton />
							<Toaster />
						</Box>
						<Sidebar />
						<Box flex="1" overflowY="auto" padding={6}>
							{children}
						</Box>
					</Flex>
				</Provider>
			</body>
		</html>
	);
}
