
// an int variable
var myInt = 42

// a string variable
var myStr = 'str'
var curliesWithNoInterp = '}{1}{'
var interp1 = 'abc${123}def'
var interp2 = '${123}def'
var interp3 = 'abc${123}'
var interp4 = 'abc${123}${456}jk$l${789}p$'
var doubleInterp = 'abc${'def${123}'}_${'${456}${789}'}'
var curliesInInterp = '{${123}{0}${true}}'

// booleans
var myTruth = true
var myFalsehood = false

// object
var myObj = {
  a: 'a'
  b: -12
  c: true
  d: !true
  list: [
    1
    2
    2+1
    {
      test: 144 > 33 && true || 99 <= 199
    }
    'a' =~ 'b'
  ]
  obj: {
    nested: [
      'hello'
    ]
  }
}

// array
var myArr = [
  'pirates'
  'say'
  'arr'
]

// array with objects
var myArrWithObjects = [
  {
    name: 'one'
    enable: true
  }
  {
    name: 'two'
    enable: false && false || 'two' !~ 'three'
  }
]

var expressionIndexOnAny = any({
})[resourceGroup().location]

var anyIndexOnAny = any(true)[any(false)]

var namedPropertyIndexer = {
}['foo']

var intIndexer = [
  's'
][0]

var functionOnIndexer1 = concat([
  's'
][0], 's')

var functionOnIndexer2 = concat([
][0], 's')

var functionOnIndexer3 = concat([
][0], any('s'))

var singleQuote = '\''
var myPropertyName = '${singleQuote}foo${singleQuote}'