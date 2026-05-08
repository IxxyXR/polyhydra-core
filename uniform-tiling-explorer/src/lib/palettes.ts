export const PALETTES = {
  vibrant: {
    name: 'Vibrant',
    colors: ['#FF9F1C', '#2EC4B6', '#FFD166', '#EF476F', '#48CAE4', '#06D6A0', '#FFFFFF', '#F1FAEE']
  },
  girih: {
    name: 'Girih Star',
    colors: ['#0047AB', '#E2B13C', '#00A19D', '#F2F2F2', '#212121', '#005F6B', '#FFD700', '#002366']
  },
  zellige: {
    name: 'Moroccan Zellige',
    colors: ['#05668D', '#F4A261', '#028090', '#00A896', '#02C39A', '#F0F3BD', '#E63946', '#2A9D8F']
  },
  alhambra: {
    name: 'Alhambra',
    colors: ['#212F45', '#F4D35E', '#8C2F39', '#B2967D', '#E6BEAE', '#4A5759', '#ED7D31', '#EE964B']
  },
  isthmus: {
    name: 'Iznik Pottery',
    colors: ['#1E3A8A', '#EF4444', '#10B981', '#F9FAFB', '#F59E0B', '#3B82F6', '#065F46', '#991B1B']
  },
  bauhaus: {
    name: 'Bauhaus',
    colors: ['#1D3557', '#E63946', '#F1FAEE', '#A8DADC', '#457B9D', '#F4A261', '#E76F51', '#2A9D8F']
  },
  midnight: {
    name: 'Deep Sea',
    colors: ['#03071E', '#F48C06', '#370617', '#6A040F', '#9D0208', '#D00000', '#DC2F02', '#E85D04']
  },
  desert: {
    name: 'Desert Bloom',
    colors: ['#E29578', '#FFFFFF', '#006D77', '#83C5BE', '#FFDDD2', '#EDF6F9', '#A8DAD2', '#2A9D8F']
  }
};

export type PaletteKey = keyof typeof PALETTES;
