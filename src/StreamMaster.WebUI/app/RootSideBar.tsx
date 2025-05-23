import MenuItemSM from "@components/MenuItemSM";
import {
	HelpIcon,
	PlayListEditorIcon,
	SDIcon,
	SettingsEditorIcon,
	SideBarMenuIcon,
	StreamingStatusIcon,
} from "@lib/common/icons";
import { useSMContext } from "@lib/context/SMProvider";

import { useLocalStorage } from "primereact/hooks";
import { Tooltip } from "primereact/tooltip";
import { useCallback } from "react";
import { Menu, MenuItem, Sidebar, sidebarClasses } from "react-pro-sidebar";
// import SDIcon from '/public/icons/sd_logo.png';

export const RootSideBar = () => {
	// const [dark, setDark] = useLocalStorage(true, 'dark');

	// const context = useContext(PrimeReactContext);

	const [collapsed, setCollapsed] = useLocalStorage<boolean>(
		true,
		"app-menu-collapsed",
	);

	const { settings } = useSMContext();

	const onsetCollapsed = useCallback(
		(isCollapsed: boolean) => {
			setCollapsed(isCollapsed);
		},
		[setCollapsed],
	);

	// const setTheme = useCallback(
	//   (intDark: boolean, callback?: () => void) => {
	//     const newTheme = intDark ? 'dark' : 'light';
	//     const theme = !intDark ? 'dark' : 'light';

	//     if (context?.changeTheme) {
	//       context.changeTheme(theme, newTheme, 'theme-link', callback);
	//     } else if (PrimeReact?.changeTheme) {
	//       PrimeReact.changeTheme(theme, newTheme, 'theme-link', callback);
	//     }
	//   },
	//   [context]
	// );

	// const toggleTheme = () => {
	//   setTheme(!dark, () => {
	//     setDark(!dark);
	//   });
	// };

	return (
		<div
			className="flex flex-column m-0 p-0 "
			style={{ height: "calc(100vh - 10px)" }}
		>
			<Sidebar
				className="app sidebar max-h-screen justify-content-start align-items-start h-full"
				collapsed={collapsed}
				collapsedWidth="40px"
				rootStyles={{
					borderRightColor: "#263238",
					[`.${sidebarClasses.container}`]: {
						backgroundColor: "var(--surface-a)",
					},
				}}
			>
				<Menu
					menuItemStyles={{
						button: ({ active }) => ({
							"&:hover": {
								backgroundColor: "#cb5e00",
								// color: '#161d21',
								filter: "brightness(140%)",
							},
							backgroundColor: active ? "#cb5e00" : undefined,
							borderBottomLeftRadius: "0.5rem",
							borderTopLeftRadius: "0.5rem",
							paddingBottom: "0.5rem",
							paddingLeft: "0",
							paddingRight: "0",
							paddingTop: "0.5rem",
						}),
					}}
				>
					<div
						onClick={() => {
							onsetCollapsed(!collapsed);
						}}
					>
						<MenuItem
							icon={
								<SideBarMenuIcon
									sx={{ color: "var(--orange-color)", fontSize: 32 }}
								/>
							}
						>
							<h2 className="orange-color">Stream Master</h2>
						</MenuItem>
					</div>
					{/* <MenuItemSM collapsed={collapsed} icon={<PlayListEditorIcon />} link="/testpanel" name='Test Panel' /> */}
					<MenuItemSM
						collapsed={collapsed}
						icon={<PlayListEditorIcon />}
						link="/editor/streams"
						name="Streams"
					/>
					<MenuItemSM
						collapsed={collapsed}
						icon={<StreamingStatusIcon />}
						link="/streamingstatus"
						name="Status"
					/>

					{/* { <MenuItemSM collapsed={collapsed} icon={<PlayListEditorIcon />} link="/editor/playlist" name="Playlist" />
        <MenuItemSM collapsed={collapsed} icon={<StreamGroupEditorIcon />} link="/editor/streamgroup" name="Stream Group" />
        <MenuItemSM collapsed={collapsed} icon={<FilesEditorIcon />} link="/editor/files" name="Files" />
        {settings.data.hls?.hlsM3U8Enable === true ? <MenuItemSM collapsed={collapsed} icon={<VideoPlayerIcon />} link="/viewer/player" name="Player" /> : null}
        {settings.data.sdSettings?.sdEnabled === true ? (
          <MenuItemSM collapsed={collapsed} icon={<SDIcon />} link="/editor/sdHeadEndLineUps" name="SD HeadEnds" />
        ) : null}
        {settings.data.sdSettings?.sdEnabled === true ? (
          <MenuItemSM collapsed={collapsed} icon={<SDChannelIcon />} link="/editor/sdChannels" name="SD Channels" />
        ) : null}
        <MenuItemSM collapsed={collapsed} icon={<StreamingStatusIcon />} link="/streamingstatus" name="Status" />
        <MenuItemSM collapsed={collapsed} icon={<QueueStatisIcon />} link="/queuestatus" name="Queue" />
        <MenuItemSM collapsed={collapsed} icon={<SettingsEditorIcon />} link="/settings" name="Settings" />
        <MenuItemSM collapsed={collapsed} icon={<LogIcon />} link="/viewer/logviewer" name="Log" /> */}
					{settings.SDSettings?.SDEnabled === true ? (
						<MenuItemSM
							collapsed={collapsed}
							icon={<SDIcon />}
							link="/editor/sdHeadEndLineUps"
							name="Schedules Direct"
						/>
					) : null}
					{/* {settings.SDSettings?.SDEnabled === true ? (
            <MenuItemSM collapsed={collapsed} icon={<SDChannelIcon />} link="/editor/sdChannels" name="SD Channels" />
          ) : null} */}
					<MenuItemSM
						collapsed={collapsed}
						icon={<SettingsEditorIcon />}
						link="/settings"
						name="Settings"
					/>
					<MenuItemSM
						collapsed={collapsed}
						icon={<HelpIcon />}
						link="https://carlreid.github.io/StreamMaster/"
						name="Wiki"
						newWindow
					/>
					{/* <MenuItem
            component={
              <SunButton
                isDark={dark}
                onClick={(e) => {
                  toggleTheme();
                }}
              />
            }
          /> */}
				</Menu>
			</Sidebar>
			<div
				className="flex flex-column pb-1 sidebar-sm-icon "
				style={{ width: "40px" }}
			>
				<div className="flex justify-content-center align-content-center">
					<img
						alt="Stream Master Logo"
						src="/images/streammaster_logo.png"
						style={{ width: "40px" }}
					/>
				</div>

				<Tooltip target=".custom-target-icon" />
				<div
					className="custom-target-icon flex justify-content-center align-content-start text-xs text-center"
					data-pr-position="right"
					data-pr-tooltip={settings.Release ?? ""}
				>
					<div className="sm-text-xs">{settings.Release ?? ""}</div>
				</div>
			</div>
		</div>
	);
};
