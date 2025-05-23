import ChannelGroupSelectorForSMChannels from '@components/channelGroups/ChannelGroupSelectorForSMChannels';
import { ColumnMeta } from '@components/smDataTable/types/ColumnMeta';
import { arraysEqual } from '@components/smDataTable/helpers/arraysEqual';
import useSelectedAndQ from '@lib/hooks/useSelectedAndQ';
import { useFilters } from '@lib/redux/hooks/filters';
import { SMChannelDto } from '@lib/smAPI/smapiTypes';
import { ColumnFilterElementTemplateOptions } from 'primereact/column';
import { DataTableFilterMetaData } from 'primereact/datatable';
import { ReactNode, useMemo } from 'react';
import SMChannelGroupEditor from './SMChannelGroupEditor';

interface SMChannelGroupColumnConfigProperties {
  readonly dataKey: string;
  readonly width: number;
}

export const useSMChannelGroupColumnConfig = ({ dataKey, width }: SMChannelGroupColumnConfigProperties) => {
  const { filters, setFilters } = useFilters(dataKey);
  const { selectedItems } = useSelectedAndQ('useSMChannelGroupColumnConfig');

  const columnConfig: ColumnMeta = useMemo(() => {
    const updateFilters = () => {
      if (selectedItems.length === 0) {
        const newFilter = { ...filters };
        const a = newFilter['Group'] as DataTableFilterMetaData;
        if (a && a.value !== undefined) {
          newFilter['Group'] = { matchMode: 'contains', value: undefined };
          setFilters(newFilter);
        }
      } else {
        const names = selectedItems.map((x) => x.Name);

        if (filters['Group']) {
          const newFilter = { ...filters };
          const a = newFilter['Group'] as DataTableFilterMetaData;
          if (!arraysEqual(a.value, names)) {
            newFilter['Group'] = { matchMode: 'contains', value: names };
            setFilters(newFilter);
          }
        }
      }
    };

    updateFilters();

    function filterTemplate(options: ColumnFilterElementTemplateOptions): ReactNode {
      return (
        <ChannelGroupSelectorForSMChannels
          dataKey="useSMChannelGroupColumnConfig"
          fixed
          onChange={(e) => {
            if (e) {
              options.filterApplyCallback();
            }
          }}
        />
      );
    }

    const bodyTemplate = (smChannelDto: SMChannelDto) => {
      return <SMChannelGroupEditor smChannelDto={smChannelDto} />;
    };

    return {
      align: 'left',
      bodyTemplate: bodyTemplate,
      field: 'Group',
      filter: true,
      filterElement: filterTemplate,
      header: 'Group',
      sortable: true,
      width: width
    } as ColumnMeta;
  }, [filters, selectedItems, setFilters, width]);

  return columnConfig;
};
