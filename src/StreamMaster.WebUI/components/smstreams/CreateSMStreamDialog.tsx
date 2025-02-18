import { SMDialogRef } from '@components/sm/SMDialog';
import SMPopUp from '@components/sm/SMPopUp';
import { SMChannelDialogRef } from '@components/smchannels/SMChannelDialog';

import { CreateSMStream } from '@lib/smAPI/SMStreams/SMStreamsCommands';
import React, { useRef, useState } from 'react';
import SMStreamDialog from './SMStreamDialog';

interface CreateSMStreamDialogProperties {
  readonly label?: string;
}

const CreateSMStreamDialog = ({ label }: CreateSMStreamDialogProperties) => {
  const [saveEnabled, setSaveEnabled] = useState<boolean>(false);
  const smChannelDialogRef = useRef<SMChannelDialogRef>(null);
  const smDialogRef = useRef<SMDialogRef>(null);

  const onSave = React.useCallback((request: any) => {
    CreateSMStream(request)
      .then(() => {})
      .catch((e: any) => {
        console.error(e);
      })
      .finally(() => {
        smDialogRef.current?.hide();
      });
  }, []);

  return (
    <SMPopUp
      buttonClassName="icon-green"
      contentWidthSize="5"
      menu
      icon="pi-plus"
      iconFilled
      buttonLabel={label}
      modal
      okButtonDisabled={!saveEnabled}
      onOkClick={() => smChannelDialogRef.current?.save()}
      placement="bottom-end"
      title="Create Stream"
    >
      <SMStreamDialog ref={smChannelDialogRef} onSave={onSave} onSaveEnabled={setSaveEnabled} />
    </SMPopUp>
  );
};

CreateSMStreamDialog.displayName = 'CreateSMStreamDialog';

export default React.memo(CreateSMStreamDialog);
