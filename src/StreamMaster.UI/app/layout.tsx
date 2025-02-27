import { Inter } from "next/font/google";
import Provider from "./provider";
import Sidebar from "../components/sidebar";
import { Box, ClientOnly, Flex, Skeleton } from "@chakra-ui/react";
import { ColorModeButton } from "../components/ui/color-mode";

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
					<Flex h="100vh" w="100%">
						<Box pos="absolute" top="4" right="4">
							<ClientOnly fallback={<Skeleton w="10" h="10" rounded="md" />}>
								<ColorModeButton />
							</ClientOnly>
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
