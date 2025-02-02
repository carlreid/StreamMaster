import '../styles/MenuItemSM.css';

import { Tooltip } from "primereact/tooltip";
import React from "react";
import { MenuItem } from "react-pro-sidebar";
import { Link, useLocation } from "react-router-dom";
import { v4 as uuidv4 } from 'uuid';

const MenuItemSM = (props: MenuItemSMProps) => {
  const tooltipClassName = React.useMemo(() => "menuitemsm-" + uuidv4(), []);
  const location = useLocation();

  if (props.collapsed) {
    return (
      <>
        <Tooltip
          target={"." + tooltipClassName}
        />
        <div
          className={tooltipClassName + " border-white"}
          data-pr-at="right+5 top"
          data-pr-hidedelay={100}
          data-pr-my="left center-2"
          data-pr-position="right"
          data-pr-showdelay={500}
          data-pr-tooltip={props.tooltip ?? props.name}
        >
          <MenuItem
            active={location.pathname === props.link}
            component={<Link className="link" target={props.newWindow === null ? "" : props.newWindow ? "_blank" : ""} to={props.link} />}
            icon={props.icon}
          >
            {props.children}
            {props.name}
          </MenuItem>
        </div>
      </>
    );
  }


  return (
    <MenuItem
      active={location.pathname === props.link}
      component={<Link className="link" target={props.newWindow === null ? "" : props.newWindow ? "_blank" : ""} to={props.link} />}
      icon={props.icon}
    >
      {props.children}
      {props.name}
    </MenuItem>
  );

}

export type MenuItemSMProps = {
  children?: React.ReactNode;
  collapsed?: boolean;
  icon: React.ReactNode;
  link: string;
  name: string;
  newWindow?: boolean;
  tooltip?: string;
}

export default React.memo(MenuItemSM);
