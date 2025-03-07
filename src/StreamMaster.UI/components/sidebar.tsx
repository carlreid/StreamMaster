"use client";

import {
	Box,
	type BoxProps,
	Link as ChakraLink,
	Flex,
	HStack,
	type LinkProps,
	Text,
} from "@chakra-ui/react";
import NextLink from "next/link";
import { usePathname } from "next/navigation";
import type { ReactElement } from "react";
import { Tooltip } from "./ui/tooltip";
import {
	LuActivity,
	LuBookOpen,
	LuSettings,
	LuSquareCode,
	LuTestTubeDiagonal,
	LuVideo,
} from "react-icons/lu";

interface NavItemProps {
	icon: ReactElement;
	children: string;
	isCollapsed: boolean;
	link: string;
	isExternal?: boolean;
	isActive?: boolean;
}

interface SidebarProps extends BoxProps {
	initialCollapsed?: boolean;
}

const NavItem: React.FC<NavItemProps> = ({
	icon,
	children,
	isCollapsed,
	link,
	isExternal = false,
	isActive = false,
}) => {
	const content = (
		<Flex
			p="4"
			borderRadius="lg"
			cursor="pointer"
			bg={isActive ? "bg.emphasized" : "transparent"}
			_hover={{ bg: isActive ? "bg.emphasized" : "bg.subtle" }}
			w="full"
		>
			<HStack>
				<Box color={isActive ? "accent.emphasized" : "inherit"}>{icon}</Box>
				{!isCollapsed && (
					<Box color={isActive ? "accent.emphasized" : "inherit"}>
						{children}
					</Box>
				)}
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

const Sidebar: React.FC<SidebarProps> = ({
	initialCollapsed = false,
	...props
}) => {
	const isCollapsed = false;
	const pathname = usePathname();

	// Use a default value during SSR to prevent layout shift
	const sidebarWidth =
		isCollapsed === null
			? initialCollapsed
				? "60px"
				: "240px"
			: isCollapsed
				? "60px"
				: "240px";

	// Helper function to check if a route is active
	const isRouteActive = (link: string): boolean => {
		if (link.startsWith("http")) return false; // External links are never active

		// Exact match for home page
		if (link === "/" && pathname === "/") return true;

		// For other routes, check if pathname starts with the link
		// This handles both exact matches and nested routes
		return link !== "/" && pathname.startsWith(link);
	};

	return (
		<Box
			minH="100vh"
			borderRight="1px"
			w={sidebarWidth}
			transition="width 0.2s ease"
			{...props}
		>
			<Flex
				h="20"
				alignItems="center"
				justifyContent={isCollapsed ? "center" : "space-between"}
				px={isCollapsed ? 0 : 4}
			>
				{!isCollapsed && (
					<ChakraLink
						as={NextLink}
						href="/"
						_hover={{ textDecoration: "none" }}
					>
						<Flex alignItems="center" gap={2}>
							<LuTestTubeDiagonal size={24} />
							<Text fontWeight="bold">StreamMaster</Text>
						</Flex>
					</ChakraLink>
				)}
				{isCollapsed && (
					<ChakraLink as={NextLink} href="/">
						<LuTestTubeDiagonal />
					</ChakraLink>
				)}
			</Flex>
			<Flex
				direction="column"
				alignContent={"center"}
				justifyContent={"center"}
				gap={1}
				px={isCollapsed ? 1 : 4}
			>
				<NavItem
					icon={<LuSquareCode size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/sources"
					isActive={isRouteActive("/sources")}
				>
					Sources
				</NavItem>
				<NavItem
					icon={<LuVideo size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/streams"
					isActive={isRouteActive("/streams")}
				>
					Streams
				</NavItem>
				<NavItem
					icon={<LuActivity size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/status"
					isActive={isRouteActive("/status")}
				>
					Status
				</NavItem>
				<NavItem
					icon={<LuSettings size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/settings"
					isActive={isRouteActive("/settings")}
				>
					Settings
				</NavItem>
				<NavItem
					icon={<LuBookOpen size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="https://carlreid.github.io/StreamMaster/"
					isExternal={true}
					isActive={false} // External links are never active
				>
					Wiki
				</NavItem>
			</Flex>
		</Box>
	);
};

export default Sidebar;
