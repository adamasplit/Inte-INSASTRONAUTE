import { readFile } from 'node:fs/promises'
import test from 'node:test'
import assert from 'node:assert/strict'

const jslibSource = await readFile(
  new URL('../Assets/Plugins/WebGL/InsastralBridge.jslib', import.meta.url),
  'utf8',
)
const reactApiBridgeSource = await readFile(
  new URL('../Assets/Scripts/Scene/STS/Api/ReactApiBridge.cs', import.meta.url),
  'utf8',
)
const collectionCardApiSource = await readFile(
  new URL('../Assets/Scripts/Scene/STS/Cards/Backend/STSCollectionCardApi.cs', import.meta.url),
  'utf8',
)

test('WebGL jslib calls the React bridge global directly', () => {
  assert.match(jslibSource, /window\.insastralUnityBridge\.request\(json\)/)
  assert.match(jslibSource, /return 0/)
  assert.doesNotMatch(jslibSource, /dispatchEvent/)
  assert.doesNotMatch(jslibSource, /insastral-request/)
})

test('React API bridge receives responses on the WebBridge GameObject', () => {
  assert.match(reactApiBridgeSource, /private const string WebBridgeGameObjectName = "WebBridge"/)
  assert.match(reactApiBridgeSource, /gameObject\.name = WebBridgeGameObjectName/)
})

test('Unity unwraps React bridge envelopes before parsing card lists', () => {
  assert.match(collectionCardApiSource, /class ReactBridgeResponse/)
  assert.match(collectionCardApiSource, /JsonConvert\.DeserializeObject<ReactBridgeResponse>/)
  assert.match(collectionCardApiSource, /TryParseCardsJson\(response\.data\.ToString\(Formatting\.None\)/)
})
