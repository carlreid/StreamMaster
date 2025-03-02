"use client";
import {
	Box,
	Heading,
	SimpleGrid,
	GridItem,
	createListCollection,
} from "@chakra-ui/react";
import type { components } from "../../lib/api.d";
import { SwitchField, InputField } from "./settings-editor";
import {
	SelectContent,
	SelectItem,
	SelectLabel,
	SelectRoot,
	SelectTrigger,
	SelectValueText,
} from "../ui/select";

const compressionOptions = createListCollection({
	items: [
		{ value: "gz", label: "GZ" },
		{ value: "zip", label: "ZIP" },
	],
});

const commandProfileOptions = createListCollection({
	items: [
		{ value: "Default", label: "Default" },
		{ value: "Redirect", label: "Redirect" },
		{ value: "SMFFMPEG", label: "SMFFMPEG" },
		{ value: "SMFFMPEGLocal", label: "SMFFMPEGLocal" },
		{ value: "YT", label: "YT" },
	],
});

const introOptions = createListCollection({
	items: [
		{ value: "None", label: "None" },
		{ value: "Once", label: "Once" },
		{ value: "Always", label: "Always" },
	],
});

const outputProfileOptions = createListCollection({
	items: [{ value: "Default", label: "Default" }],
});

export const AdvancedSettings = ({
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
				Advanced Settings
			</Heading>
			<SimpleGrid columns={{ base: 1, md: 2 }} gap={4}>
				<GridItem>
					<SwitchField
						label="Clean URLs"
						isChecked={settings.cleanURLs}
						onChange={(checked) => handleChange("cleanURLs", checked)}
						helperText="Remove unnecessary parameters from URLs for better compatibility with some web servers."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Custom User Agent"
						value={settings.clientUserAgent}
						onChange={(value) => handleChange("clientUserAgent", value)}
						helperText="Specify a custom identity for outgoing requests."
					/>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"defaultCompression",
								value[0] as components["schemas"]["SettingDto"]["defaultCompression"],
							)
						}
						value={[settings.defaultCompression || "GZ"]}
						collection={compressionOptions}
					>
						<SelectLabel>File Compression Format</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Compression Format" />
						</SelectTrigger>
						<SelectContent>
							{compressionOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						Choose how files are compressed when downloaded or stored (GZ is
						generally faster).
					</Box>
				</GridItem>
				<GridItem>
					<InputField
						label="Default Logo Path"
						value={settings.defaultLogo}
						onChange={(value) => handleChange("defaultLogo", value)}
						helperText="Path or URL to the logo shown when no channel-specific logo is available."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Device Identifier"
						value={settings.deviceID}
						onChange={(value) => handleChange("deviceID", value)}
						helperText="Unique identifier for this device. Used for authentication and tracking."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Database Debugging"
						isChecked={settings.enableDBDebug}
						onChange={(checked) => handleChange("enableDBDebug", checked)}
						helperText="Show detailed database operations in logs. Only enable when troubleshooting database issues."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Enable Secure Connections (SSL/TLS)"
						isChecked={settings.enableSSL}
						onChange={(checked) => handleChange("enableSSL", checked)}
						helperText="Encrypt connections to this server. Requires valid certificate configuration below."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Maximum Concurrent Streams"
						type="number"
						value={settings.globalStreamLimit}
						onChange={(value) => handleChange("globalStreamLimit", value)}
						helperText="Limit how many streams can run simultaneously to prevent server overload."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Icon Cache Duration (Days)"
						type="number"
						value={settings.iconCacheExpirationDays}
						onChange={(value) => handleChange("iconCacheExpirationDays", value)}
						helperText="How long to keep channel icons in cache before refreshing them. Lower values use more bandwidth."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Enable Logo Caching"
						isChecked={settings.logoCache}
						onChange={(checked) => handleChange("logoCache", checked)}
						helperText="Store logos locally to improve loading speed and reduce bandwidth usage."
					/>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"m3U8OutPutProfile",
								value[0] as components["schemas"]["SettingDto"]["m3U8OutPutProfile"],
							)
						}
						value={[settings.m3U8OutPutProfile || "SMFFMPEG"]}
						collection={commandProfileOptions}
					>
						<SelectLabel>HLS Stream Fallback Profile</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Fallback Profile for HLS Streams" />
						</SelectTrigger>
						<SelectContent>
							{commandProfileOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						Processing method to use when a stream's native profile isn't
						supported. SMFFMPEG is recommended for most users.
					</Box>
				</GridItem>
				<GridItem>
					<InputField
						label="Maximum Parallel Downloads"
						type="number"
						value={settings.maxConcurrentDownloads}
						onChange={(value) => handleChange("maxConcurrentDownloads", value)}
						helperText="Limit simultaneous downloads to prevent network congestion and server overload."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Connection Retry Attempts"
						type="number"
						value={settings.maxConnectRetry}
						onChange={(value) => handleChange("maxConnectRetry", value)}
						helperText="How many times to attempt reconnecting to a stream before giving up."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Connection Retry Timeout (ms)"
						type="number"
						value={settings.maxConnectRetryTimeMS}
						onChange={(value) => handleChange("maxConnectRetryTimeMS", value)}
						helperText="Maximum time in milliseconds to wait between connection retry attempts."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Maximum Log File Size (MB)"
						type="number"
						value={settings.maxLogFileSizeMB}
						onChange={(value) => handleChange("maxLogFileSizeMB", value)}
						helperText="Size at which log files will rotate to prevent excessive disk usage."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Log File History Count"
						type="number"
						value={settings.maxLogFiles}
						onChange={(value) => handleChange("maxLogFiles", value)}
						helperText="Number of old log files to keep before deleting. Higher values use more disk space."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Restart Limit"
						type="number"
						value={settings.maxStreamReStart}
						onChange={(value) => handleChange("maxStreamReStart", value)}
						helperText="Maximum number of times a stream will automatically restart if it fails."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Show Client Hostnames"
						isChecked={settings.showClientHostNames}
						onChange={(checked) => handleChange("showClientHostNames", checked)}
						helperText="Display device names instead of IP addresses in connection logs and statistics."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Show Message Videos"
						isChecked={settings.showMessageVideos}
						onChange={(checked) => handleChange("showMessageVideos", checked)}
						helperText="Display a video when no streams are available."
					/>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"showIntros",
								value[0] as components["schemas"]["SettingDto"]["showIntros"],
							)
						}
						value={[settings.showIntros || "None"]}
						collection={introOptions}
					>
						<SelectLabel>Channel Intro Videos</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Intro Video Behavior" />
						</SelectTrigger>
						<SelectContent>
							{introOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						Control when channel intro videos play: Never, First-time only, or
						Every time a channel is viewed.
					</Box>
				</GridItem>
				<GridItem>
					<InputField
						label="SSL Certificate Password"
						value={settings.sslCertPassword}
						onChange={(value) => handleChange("sslCertPassword", value)}
						helperText="Password to unlock your SSL certificate (if password-protected)."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="SSL Certificate Location"
						value={settings.sslCertPath}
						onChange={(value) => handleChange("sslCertPath", value)}
						helperText="Full path to your SSL certificate file (.pfx or .p12 format)."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Web Interface Directory"
						value={settings.uiFolder}
						onChange={(value) => handleChange("uiFolder", value)}
						helperText="Folder containing the web interface files. Only change if using a custom UI."
					/>
				</GridItem>
				<GridItem>
					<SwitchField
						label="Always Use EPG Logos for Streams"
						isChecked={settings.videoStreamAlwaysUseEPGLogo}
						onChange={(checked) =>
							handleChange("videoStreamAlwaysUseEPGLogo", checked)
						}
						helperText="Prefer logos from your EPG data over any other logo sources for video streams."
					/>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"defaultCommandProfileName",
								value[0] as components["schemas"]["SettingDto"]["defaultCommandProfileName"],
							)
						}
						value={[settings.defaultCommandProfileName || "Default"]}
						collection={commandProfileOptions}
					>
						<SelectLabel>Default Stream Processing Profile</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Default Processing Method" />
						</SelectTrigger>
						<SelectContent>
							{commandProfileOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						Processing method used for streams that don't specify their own
						profile. SMFFMPEG recommended for most users, and is default.
					</Box>
				</GridItem>
				<GridItem>
					<SelectRoot
						onValueChange={({ value }) =>
							handleChange(
								"defaultOutputProfileName",
								value[0] as components["schemas"]["SettingDto"]["defaultOutputProfileName"],
							)
						}
						value={[settings.defaultOutputProfileName || "Default"]}
						collection={outputProfileOptions}
					>
						<SelectLabel>Default Output Format Profile</SelectLabel>
						<SelectTrigger>
							<SelectValueText placeholder="Select Default Output Format" />
						</SelectTrigger>
						<SelectContent>
							{outputProfileOptions.items.map((option) => (
								<SelectItem key={option.value} item={option}>
									{option.label}
								</SelectItem>
							))}
						</SelectContent>
					</SelectRoot>
					<Box fontSize="xs" color="gray.500">
						Controls quality, resolution and format settings for streams without
						specific output profiles.
					</Box>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Read Timeout (ms)"
						type="number"
						value={settings.streamReadTimeOutMs}
						onChange={(value) => handleChange("streamReadTimeOutMs", value)}
						helperText="How long to wait for data from a stream before considering it stalled."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Startup Timeout (ms)"
						type="number"
						value={settings.streamStartTimeoutMs}
						onChange={(value) => handleChange("streamStartTimeoutMs", value)}
						helperText="Maximum time to wait for a stream to begin playing before giving up."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Client Connection Timeout (ms)"
						type="number"
						value={settings.clientReadTimeoutMs}
						onChange={(value) => handleChange("clientReadTimeoutMs", value)}
						helperText="How long to maintain inactive client connections before disconnecting them."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Retry Count"
						type="number"
						value={settings.streamRetryLimit}
						onChange={(value) => handleChange("streamRetryLimit", value)}
						helperText="Maximum number of times to retry a failed stream within the retry period."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Retry Window (Hours)"
						type="number"
						value={settings.streamRetryHours}
						onChange={(value) => handleChange("streamRetryHours", value)}
						helperText="Time period during which stream retry attempts are counted toward the limit."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="Stream Shutdown Delay (ms)"
						type="number"
						value={settings.streamShutDownDelayMs}
						onChange={(value) => handleChange("streamShutDownDelayMs", value)}
						helperText="Grace period before closing a stream after all viewers have disconnected."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="FFmpeg Path"
						value={settings.ffmPegExecutable}
						onChange={(value) => handleChange("ffmPegExecutable", value)}
						helperText="Full path to the FFmpeg executable for video processing."
					/>
				</GridItem>
				<GridItem>
					<InputField
						label="FFprobe Path"
						value={settings.ffProbeExecutable}
						onChange={(value) => handleChange("ffProbeExecutable", value)}
						helperText="Full path to the FFprobe executable for media analysis."
					/>
				</GridItem>
			</SimpleGrid>
		</Box>
	);
};
