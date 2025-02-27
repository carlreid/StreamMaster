"use client";

import {
	Box,
	ClientOnly,
	Flex,
	IconButton,
	Link as ChakraLink,
	Skeleton,
	type BoxProps,
	type LinkProps,
	Text,
	HStack,
} from "@chakra-ui/react";
import { useState, type ReactElement } from "react";
import {
	FiMenu,
	FiActivity,
	FiSettings,
	FiBookOpen,
	FiVideo,
} from "react-icons/fi";
import { Tooltip } from "./ui/tooltip";
import { useColorMode } from "./ui/color-mode";
import NextLink from "next/link";

interface NavItemProps {
	icon: ReactElement;
	children: string;
	isCollapsed: boolean;
	link: string;
	isExternal?: boolean;
}

interface SidebarProps extends BoxProps {
	// Add any additional props here
}

const NavItem: React.FC<NavItemProps> = ({
	icon,
	children,
	isCollapsed,
	link,
	isExternal = false,
}) => {
	const content = (
		<Flex
			p="4"
			borderRadius="lg"
			cursor="pointer"
			_hover={{ bg: "gray.700" }}
			w="full"
		>
			<HStack>
				<Box>{icon}</Box>
				{!isCollapsed && <Box>{children}</Box>}
			</HStack>
		</Flex>
	);

	const linkProps: LinkProps = {
		href: link,
		style: { textDecoration: "none" },
	};

	if (isExternal) {
		linkProps.target = "_blank";
		linkProps.rel = "noopener noreferrer";
	}

	return (
		<Tooltip content={isCollapsed ? children : ""} disabled={!isCollapsed}>
			<ChakraLink asChild {...linkProps}>
				<NextLink href={link}>{content}</NextLink>
			</ChakraLink>
		</Tooltip>
	);
};

const Sidebar: React.FC<SidebarProps> = (props) => {
	const [isCollapsed, setIsCollapsed] = useState<boolean>(false);
	const { colorMode } = useColorMode();

	const bg = colorMode === "dark" ? "gray.800" : "white";
	const borderColor = colorMode === "dark" ? "gray.700" : "gray.200";

	return (
		<ClientOnly
			fallback={
				<Skeleton boxSize="8" w={isCollapsed ? "60px" : "240px"} minH="100vh" />
			}
		>
			<Box
				minH="100vh"
				bg={bg}
				borderRight="1px"
				borderRightColor={borderColor}
				w={isCollapsed ? "60px" : "240px"}
				transition="width 0.2s ease"
				{...props}
			>
				<Flex
					h="20"
					alignItems="center"
					justifyContent={isCollapsed ? "center" : "space-between"}
					px={isCollapsed ? 0 : 4}
				>
					<Text hidden={isCollapsed}>StreamMaster</Text>
					<IconButton
						aria-label="Toggle Sidebar"
						variant="ghost"
						onClick={() => setIsCollapsed(!isCollapsed)}
					>
						<FiMenu />
					</IconButton>
				</Flex>
				<Flex
					direction="column"
					alignContent={"center"}
					justifyContent={"center"}
					gap={1}
					px={isCollapsed ? 1 : 4}
				>
					<NavItem
						icon={<FiVideo size={20} />}
						isCollapsed={isCollapsed}
						link="/streams"
					>
						Streams
					</NavItem>
					<NavItem
						icon={<FiActivity size={20} />}
						isCollapsed={isCollapsed}
						link="/status"
					>
						Status
					</NavItem>
					<NavItem
						icon={<FiSettings size={20} />}
						isCollapsed={isCollapsed}
						link="/settings"
					>
						Settings
					</NavItem>
					<NavItem
						icon={<FiBookOpen size={20} />}
						isCollapsed={isCollapsed}
						link="https://carlreid.github.io/StreamMaster/"
						isExternal={true}
					>
						Wiki
					</NavItem>
				</Flex>
			</Box>
		</ClientOnly>
	);
};

export default Sidebar;
