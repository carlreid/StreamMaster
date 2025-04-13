import { GetMessage } from "@lib/common/intl";
import { useSettingsContext } from "@lib/context/SettingsProvider";
import { Fieldset } from "primereact/fieldset";
import React from "react";
import { BaseSettings } from "./BaseSettings";
import { GetCheckBoxLine } from "./components/GetCheckBoxLine";

export function SecuritySettings(): React.ReactElement {
	const { currentSetting } = useSettingsContext();

	if (currentSetting === null || currentSetting === undefined) {
		return (
			<Fieldset className="mt-4 pt-10" legend={GetMessage("SD")}>
				<div className="text-center">{GetMessage("loading")}</div>
			</Fieldset>
		);
	}

	return (
		<BaseSettings title="SECURITY">
			{GetCheckBoxLine({
				field: "EnableShortLinks",
			})}
		</BaseSettings>
	);
}
