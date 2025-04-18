import SMPopUp from '@components/sm/SMPopUp';
import { useIsTrue } from '@lib/redux/hooks/isTrue';
import { useSelectedItems } from '@lib/redux/hooks/selectedItems';
import { DeleteSMChannel } from '@lib/smAPI/SMChannels/SMChannelsCommands';
import { DeleteSMChannelRequest, SMChannelDto, SMStreamDto } from '@lib/smAPI/smapiTypes';
import React, { useState } from 'react';

interface DeleteSMChannelProperties {
  readonly smChannel: SMChannelDto;
  readonly onDelete: () => void;
}

const DeleteSMChannelDialog = ({ onDelete, smChannel }: DeleteSMChannelProperties) => {
  const dataKey = 'SMChannelSMStreamDialog-SMStreamDataForSMChannelSelector';
  const { isTrue: smTableIsSimple } = useIsTrue('isSimple');
  const { setSelectedItems } = useSelectedItems<SMStreamDto>(dataKey);
  const [isCalled, setIsCalled] = useState(false);
  const ReturnToParent = React.useCallback(() => {
    setSelectedItems([]);
    setIsCalled(false);
    onDelete();
  }, [onDelete, setSelectedItems]);

  const accept = React.useCallback(() => {
    if (isCalled) return;
    setIsCalled(true);

    const toSend = {} as DeleteSMChannelRequest;
    toSend.SMChannelId = smChannel.Id;
    DeleteSMChannel(toSend)
      .then((response) => {
        console.log('Delete Channel');
      })
      .catch((error) => {
        console.error('Delete Channel', error.message);
      })
      .finally(() => {
        ReturnToParent();
      });
  }, [ReturnToParent, isCalled, smChannel.Id]);

  return (
    <SMPopUp
      placement={smTableIsSimple ? 'bottom-end' : 'bottom'}
      // rememberKey={'DeleteSMChannelDialog'}
      title="Delete?"
      info=""
      onOkClick={() => accept()}
      buttonClassName="icon-red"
      icon="pi-times"
    >
      <div className="sm-center-stuff">
        <div className="text-container">{smChannel.Name}</div>
      </div>
    </SMPopUp>
  );
};

DeleteSMChannelDialog.displayName = 'DeleteSMChannelDialog';

export default React.memo(DeleteSMChannelDialog);
