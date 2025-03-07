"use client";

import { Button, HStack } from "@chakra-ui/react";
import { useEffect, useState } from "react";
import { useApi } from "../../lib/use-api";
import { Tooltip } from "../ui/tooltip";
import { toaster } from "../ui/toaster";
import { LuLink } from "react-icons/lu";

export const QuickAccessUrls = () => {
	const [links, setLinks] = useState<{
		m3uLink?: string;
		xmlLink?: string;
		hdhrLink?: string;
	}>({});

	const { data, error } = useApi("/api/streamgroups/getpagedstreamgroups", {
		params: {
			query: {
				PageNumber: 1,
				PageSize: 100, // Large enough to include all groups
			},
		},
	});

	useEffect(() => {
		if (data?.data) {
			// Find the "ALL" group or the first system group
			const allGroup = data.data.find(
				(group) => group.name === "ALL" || group.isSystem,
			);

			if (allGroup) {
				setLinks({
					m3uLink: allGroup.m3ULink,
					xmlLink: allGroup.xmlLink,
					hdhrLink: allGroup.hdhrLink,
				});
			}
		}
	}, [data]);

	const copyToClipboard = (text: string | undefined, label: string) => {
		if (!text) return;

		navigator.clipboard.writeText(text);
		toaster.create({
			title: "Copied to clipboard",
			description: `${label} link has been copied`,
			duration: 2000,
		});
	};

	if (error) {
		return <div>Error loading stream group links</div>;
	}

	return (
		<HStack gap={3} wrap="wrap">
			{links.m3uLink && (
				<Tooltip content="Copy M3U playlist link">
					<Button
						aria-label="M3U Link"
						colorPalette="blue"
						variant="outline"
						onClick={() => copyToClipboard(links.m3uLink, "M3U")}
					>
						<LuLink style={{ marginRight: "8px" }} /> M3U URL
					</Button>
				</Tooltip>
			)}

			{links.xmlLink && (
				<Tooltip content="Copy EPG/XML guide link">
					<Button
						aria-label="EPG/XML Link"
						colorPalette="green"
						variant="outline"
						onClick={() => copyToClipboard(links.xmlLink, "EPG/XML")}
					>
						<LuLink style={{ marginRight: "8px" }} /> EPG URL
					</Button>
				</Tooltip>
			)}

			{links.hdhrLink && (
				<Tooltip content="Copy HDHR link">
					<Button
						aria-label="HDHR Link"
						colorPalette="purple"
						variant="outline"
						onClick={() => copyToClipboard(links.hdhrLink, "HDHR")}
					>
						<LuLink style={{ marginRight: "8px" }} /> HDHR URL
					</Button>
				</Tooltip>
			)}
		</HStack>
	);
};
