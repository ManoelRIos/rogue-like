// Temas visuais e de dificuldade de cada um dos 5 andares.
export const THEMES = [
  {
    name: 'Masmorra Ferrugem', floor: '#4b3a31', floorAlt: '#56443a', grout: '#2c211c', wall: '#7a4f3a', wallD: '#4e3226',
    top: '#2a1c17', fog: 0x140b0b, ambient: 0xffd2b0, ambientI: 1.7, torch: 0xff9a4a, pit: '#070405',
    enemies: [['gosma', 5], ['cogumelo', 3], ['caveira', 2], ['morcego', 2]], boss: 'reiGosma',
  },
  {
    name: 'Catacumbas de Osso', floor: '#3e4150', floorAlt: '#474b5c', grout: '#24263a', wall: '#5e6378', wallD: '#3b3f52',
    top: '#1b1d2a', fog: 0x0b0c14, ambient: 0xc4d0ff, ambientI: 1.6, torch: 0x8ab4ff, pit: '#04040a',
    enemies: [['caveira', 5], ['morcego', 3], ['lanterna', 3], ['gosma', 2], ['cogumelo', 2]], boss: 'sinoFantasma',
  },
  {
    name: 'Forja Rubra', floor: '#4a2b27', floorAlt: '#55322c', grout: '#2a1412', wall: '#80402f', wallD: '#55271c',
    top: '#2a100c', fog: 0x160806, ambient: 0xffb8a0, ambientI: 1.6, torch: 0xff6a2a, pit: '#2a0400', lava: true,
    enemies: [['torreta', 3], ['besouro', 3], ['caveira', 3], ['lanterna', 3], ['morcego', 2]], boss: 'golem',
  },
  {
    name: 'Cripta Esmeralda', floor: '#2f4538', floorAlt: '#36503f', grout: '#18271e', wall: '#3f6c54', wallD: '#284636',
    top: '#13241b', fog: 0x06100b, ambient: 0xbaffd8, ambientI: 1.55, torch: 0x7dffb0, pit: '#020604',
    enemies: [['cristal', 4], ['besouro', 3], ['torreta', 3], ['cogumelo', 3], ['lanterna', 2]], boss: 'aranha',
  },
  {
    name: 'Altar do Último Tiro', floor: '#3a2e4a', floorAlt: '#433555', grout: '#211a2c', wall: '#5e4884', wallD: '#3c2d58',
    top: '#1b1328', fog: 0x0c0714, ambient: 0xe0c8ff, ambientI: 1.55, torch: 0xd08aff, pit: '#050208',
    enemies: [['cristal', 4], ['besouro', 3], ['torreta', 3], ['caveira', 3], ['lanterna', 3], ['morcego', 2]], boss: 'sacerdote',
  },
];
