'use strict';

/**
 * Authored line bank + chronicle templates (Person C content deliverable).
 * Short, on-topic, bounded. Replaces any need for generated text.
 * No brands, no real people, English only.
 */

const LINES = {
  'grateful:sun': [
    'Warm sun! My field thanks you, great one.',
    'You smiled on us today. The crops stand tall.',
  ],
  'grateful:rain': [
    'Gentle rain, full jars. You provide.',
    'Just enough rain. You listen well.',
  ],
  'content:work': [
    'Off to the fields. It will be a good day.',
    'Nets mended, tools sharp. We work.',
  ],
  'worried:cold': [
    'My fingers are numb. May we have sun?',
    'The little one shivers at night. Please, warmth.',
  ],
  'worried:hunger': [
    'The grain jar is low. We pray for sun.',
    'Little grows since the floods. Help us.',
  ],
  'worried:flood': [
    'Water at the door! Lift the rain, I beg you.',
    'The river climbs. We are afraid.',
  ],
  'angry:flood': [
    'You drowned my harvest! Was this deserved?',
    'My farm is a lake. I am angry, sky-god.',
  ],
  'angry:drought': [
    'Cracked earth again. Do you even look down?',
    'The well is dust. Remember us!',
  ],
  'devout:pray': [
    'Small offering, great faith. Watch over us.',
    'For sun in season and rain in measure, we pray.',
  ],
};

const CHRONICLE_TEMPLATES = {
  rain: 'Rain drummed the thatch [{id}].',
  flood: 'The low hut flooded and {target} sheltered [{id}].',
  crops: 'The fields greened under kind rain [{id}].',
  drought: 'The soil cracked and jars ran low [{id}].',
  calm: 'The sky cleared but the damage remained [{id}].',
  pray: '{target} knelt at the shrine marker [{id}].',
  shelter: '{target} fled to dry shelter [{id}].',
  tend: 'Hands mended what the sky had hurt [{id}].',
  faithUp: 'Faith burns brighter.',
  faithDown: 'Doubt creeps between the huts.',
  steady: 'The village endures.',
};

function pickLine(mood, topic, rng) {
  const bank = LINES[mood + ':' + topic] || LINES['content:work'];
  return bank[Math.floor(rng.next() * bank.length)];
}

module.exports = { LINES, CHRONICLE_TEMPLATES, pickLine };
