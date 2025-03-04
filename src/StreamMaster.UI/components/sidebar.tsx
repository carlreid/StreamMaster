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
import type { ReactElement } from "react";
import { Tooltip } from "./ui/tooltip";
import {
	LuActivity,
	LuBookOpen,
	LuSettings,
	LuSquareCode,
	LuVideo,
} from "react-icons/lu";

interface NavItemProps {
	icon: ReactElement;
	children: string;
	isCollapsed: boolean;
	link: string;
	isExternal?: boolean;
}

interface SidebarProps extends BoxProps {
	// Add any additional props here
	initialCollapsed?: boolean;
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
			_hover={{ bg: "bg.emphasized" }}
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

const Sidebar: React.FC<SidebarProps> = ({
	initialCollapsed = false,
	...props
}) => {
	const isCollapsed = false;

	// Use a default value during SSR to prevent layout shift
	const sidebarWidth =
		isCollapsed === null
			? initialCollapsed
				? "60px"
				: "240px"
			: isCollapsed
				? "60px"
				: "240px";

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
				<Text hidden={isCollapsed || false}>StreamMaster</Text>
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
				>
					Sources
				</NavItem>
				<NavItem
					icon={<LuVideo size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/streams"
				>
					Streams
				</NavItem>
				<NavItem
					icon={<LuActivity size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/status"
				>
					Status
				</NavItem>
				<NavItem
					icon={<LuSettings size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="/settings"
				>
					Settings
				</NavItem>
				<NavItem
					icon={<LuBookOpen size={20} />}
					isCollapsed={isCollapsed ?? initialCollapsed}
					link="https://carlreid.github.io/StreamMaster/"
					isExternal={true}
				>
					Wiki
				</NavItem>
			</Flex>
		</Box>
	);
};

export default Sidebar;
