import { ReVxeGrid } from '#/components/grid';
import { ReDrawer, ReModal } from '#/components/modal';
import { ReDictionary } from '#/components/dictionary';

declare module 'vue' {
  export interface GlobalComponents {
    ReVxeGrid: typeof ReVxeGrid;
    ReModal: typeof ReModal;
    ReDrawer: typeof ReDrawer;
    ReDictionary: typeof ReDictionary;
  }
}

export {};
