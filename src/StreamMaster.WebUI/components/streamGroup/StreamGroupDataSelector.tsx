import StringEditor from '@components/inputs/StringEditor';
import SMDataTable from '@components/smDataTable/SMDataTable';
import { ColumnMeta } from '@components/smDataTable/types/ColumnMeta';
import StreamGroupDeleteDialog from '@components/streamGroup/StreamGroupDeleteDialog';
import VButton from '@components/vs/VButton';
import { Logger } from '@lib/common/logger';
import { useSelectedItems } from '@lib/redux/hooks/selectedItems';
import { useSelectedStreamGroup } from '@lib/redux/hooks/selectedStreamGroup';
import { UpdateStreamGroup } from '@lib/smAPI/StreamGroups/StreamGroupsCommands';
import useGetPagedStreamGroups from '@lib/smAPI/StreamGroups/useGetPagedStreamGroups';
import { StreamGroupDto, UpdateStreamGroupRequest } from '@lib/smAPI/smapiTypes';
import { DataTableRowClickEvent, DataTableRowData, DataTableRowExpansionTemplate } from 'primereact/datatable';
import { memo, useCallback, useMemo } from 'react';
import StreamGroupDataSelectorValue from './StreamGroupDataSelectorValue';
import { useStreamGroupDeviceIDColumnConfig } from './columns/useStreamGroupDeviceIDColumnConfig';
import BooleanEditor from '@components/inputs/BooleanEditor';

export interface StreamGroupDataSelectorProperties {
  readonly id: string;
}

const StreamGroupDataSelector = ({ id }: StreamGroupDataSelectorProperties) => {
  const { selectedStreamGroup, setSelectedStreamGroup } = useSelectedStreamGroup('StreamGroup');
  const { isLoading } = useGetPagedStreamGroups();
  const { setSelectedItems } = useSelectedItems('selectedStreamGroup');

  const streamGroupDeviceIDColumnConfig = useStreamGroupDeviceIDColumnConfig({ width: 60 });
  const rowExpansionTemplate = (rowData: DataTableRowData<any>, options: DataTableRowExpansionTemplate) => {
    const streamGroupDto = rowData as unknown as StreamGroupDto;
    Logger.debug('StreamGroupDataSelector rowExpansionTemplate', streamGroupDto);
    return (
      <div className="ml-3 m-1">
        <StreamGroupDataSelectorValue streamGroupDto={streamGroupDto} id={streamGroupDto.Id + '-streams'} />
      </div>
    );
  };

  const actionTemplate = useCallback((streamGroupDto: StreamGroupDto) => {
    if (streamGroupDto.IsReadOnly === true) {
      return (
        <div className="sm-end-stuff">
          <VButton streamGroupDto={null} />
        </div>
      );
    }
    return (
      <div className="sm-end-stuff pl-2">
        <StreamGroupDeleteDialog streamGroup={streamGroupDto} zIndex={12} />
        <VButton streamGroupDto={streamGroupDto} />

        {/* <M3UFileRefreshDialog selectedFile={rowData} />
         <M3UFileRemoveDialog selectedFile={rowData} /> */}
        {/* <EPGFileEditDialog selectedFile={rowData} /> */}
      </div>
    );
  }, []);

  const update = useCallback((request: UpdateStreamGroupRequest) => {
    console.log('update', request);

    UpdateStreamGroup(request)
      .then((res) => {})
      .catch((error) => {
        console.log('error', error);
      })
      .finally();
  }, []);

  const nameTemplate = useCallback(
    (rowData: StreamGroupDto) => {
      if (rowData.IsReadOnly === true || rowData.Name.toLowerCase() === 'default') {
        return <div className="text-container pl-1">{rowData.Name}</div>;
      }
      return (
        <StringEditor
          value={rowData.Name}
          onSave={(e) => {
            if (e !== undefined) {
              const ret = { NewName: e, StreamGroupId: rowData.Id } as UpdateStreamGroupRequest;
              update(ret);
            }
          }}
        />
      );
    },
    [update]
  );

  const groupKeyTemplate = useCallback(
    (rowData: StreamGroupDto) => {
      return (
        <StringEditor
          value={rowData.GroupKey}
          onSave={(e) => {
            if (e !== undefined) {
              const ret = { GroupKey: e, StreamGroupId: rowData.Id } as UpdateStreamGroupRequest;
              update(ret);
            }
          }}
        />
      );
    },
    [update]
  );

  const strmTemplate = useCallback(
    (rowData: StreamGroupDto) => {
      if (rowData.Name.toLocaleLowerCase() === 'all') {
        return <></>;
      }
      return (
        <div className="sm-center-stuff">
          <BooleanEditor
            checked={rowData.CreateSTRM}
            onChange={(e) => {
              if (e !== undefined) {
                const ret = { CreateSTRM: e, StreamGroupId: rowData.Id } as UpdateStreamGroupRequest;
                update(ret);
              }
            }}
          />
        </div>
      );
    },
    [update]
  );

  const columns = useMemo(
    (): ColumnMeta[] => [
      {
        bodyTemplate: nameTemplate,
        field: 'Name',
        filter: true,
        sortable: true,
        width: 70
      },
      {
        bodyTemplate: groupKeyTemplate,
        field: 'GroupKey',
        width: 76
      },
      {
        align: 'center',
        alignHeader: 'center',
        bodyTemplate: strmTemplate,
        field: 'CreateSTRM',
        header: 'STRM',
        width: 16
      },
      {
        align: 'right',
        field: 'ChannelCount',
        header: 'Chs',
        width: 20
      },
      {
        field: 'HDHRLink',
        fieldType: 'url',
        width: 16
      },

      {
        align: 'center',
        field: 'XMLLink',
        fieldType: 'epglink',
        width: 16
      },
      {
        field: 'M3ULink',
        fieldType: 'm3ulink',
        width: 16
      },
      streamGroupDeviceIDColumnConfig,
      {
        align: 'center',
        bodyTemplate: actionTemplate,
        field: 'autoUpdate',
        header: '',
        width: 22
      }
    ],
    [nameTemplate, groupKeyTemplate, streamGroupDeviceIDColumnConfig, actionTemplate]
  );

  return (
    <SMDataTable
      columns={columns}
      defaultSortField="Name"
      defaultSortOrder={1}
      emptyMessage="No Stream Groups"
      enableClick
      enableExport={false}
      id={id}
      isLoading={isLoading}
      noSourceHeader
      showExpand
      onRowClick={(e: DataTableRowClickEvent) => {
        if (e.data.Id !== selectedStreamGroup?.Id) {
          console.log('StreamGroupDataSelector', e.data);
          setSelectedStreamGroup(e.data as StreamGroupDto);
          setSelectedItems([e.data as StreamGroupDto]);
        }
      }}
      rowExpansionTemplate={rowExpansionTemplate}
      queryFilter={useGetPagedStreamGroups}
      selectedItemsKey="selectedStreamGroup"
      selectionMode="single"
      selectRow
      style={{ height: '30vh' }}
    />
  );
};

StreamGroupDataSelector.displayName = 'Stream Group Editor';

export default memo(StreamGroupDataSelector);
