# 小学校1年～中学校3年カリキュラム対応表

この文書は、Kids Training の単元カタログと現行の小学校・中学校学習指導要領との対応を示す。実装上の唯一の正本は `CurriculumPolicy*.cs` の `CurriculumUnit` であり、全単元が安定した `unitId`、教科、領域、実学年、順序、前提単元、生成器、評価方式、根拠資料を持つ。

登録学校学年は保護者向けの表示・記録属性であり、出題上限ではない。通常モードは登録学年にかかわらず小学1年から開始し、「登録学年の単元を優先する」をONにした場合だけ登録学年から開始する。どちらのモードでも教科別の順序を保ちながら、現在学年の有効な全単元が難易度5を合格して定着確認へ入るまで次学年を解放しない。無効な単元は完了条件から除外し、上位学年の履歴や保存セッションは進級ゲートを迂回できない。1日・3日・7日後の定着確認は、すでに解放された同一学年内の学習を妨げない。

特別活動・家庭科（中学校の家庭分野を含む）・道徳は全学年で出題・選択設定・進級条件から除外する。過去の履歴と獲得済みXP・星は保持する。削除科目を含む保存セッションは再開せず開始画面へ戻す。

## 根拠資料

- [学習指導要領・解説一覧](https://www.mext.go.jp/a_menu/shotou/new-cs/1384661.htm)
- [国語編](https://www.mext.go.jp/content/20220606-mxt_kyoiku02-100002607_002.pdf)
- [算数編](https://www.mext.go.jp/content/20211102-mxt_kyoiku02-100002607_04.pdf)
- [理科編](https://www.mext.go.jp/content/20211020-mxt_kyoiku02-100002607_05.pdf)
- [社会編](https://www.mext.go.jp/content/20230308-mxt_kyoiku02-100002607_003.pdf)
- [外国語活動・外国語編](https://www.mext.go.jp/content/20220614-mxt_kyoiku02-100002607_11.pdf)
- [家庭編](https://www.mext.go.jp/content/20240918-mxt_kyoiku01-100002607_02.pdf)
- [中学校学習指導要領・解説一覧](https://www.mext.go.jp/a_menu/shotou/new-cs/1387016.htm)
- [中学校 国語編](https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_002.pdf)
- [中学校 社会編](https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_003.pdf)
- [中学校 数学編](https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_004.pdf)
- [中学校 理科編](https://www.mext.go.jp/content/20210830-mxt_kyoiku01-100002608_05.pdf)
- [中学校 技術・家庭編](https://www.mext.go.jp/component/a_menu/education/micro_detail/__icsFiles/afieldfile/2019/03/18/1387018_009.pdf)
- [中学校 外国語編](https://www.mext.go.jp/content/20210531-mxt_kyoiku01-100002608_010.pdf)

## 1～3年の単元

1～3年の問題は、次の規則で一問一答・選択・入力式の自動採点単元へ割り当てる。IDはすべて `教科.g学年.トピック` で、表中の並びが教科レーン内の順序になる。各IDの公式根拠は、算数・国語・社会・理科・外国語は上記の各教科解説、それ以外は学習指導要領・解説一覧である。

| 学年 | 教科 | 単元ID（順序） | 評価 |
|---|---|---|---|
| 1 | 算数 | `math.g1.kazu`, `math.g1.shape`, `math.g1.add`, `math.g1.sub`, `math.g1.clock`, `math.g1.measure`, `math.g1.story`, `math.g1.money`, `math.g1.groups`, `math.g1.chart` | 自動採点 |
| 1 | 国語 | `japanese.g1.moji`, `japanese.g1.bun`, `japanese.g1.kokugo`, `japanese.g1.goi`, `japanese.g1.dokkai` | 自動採点 |
| 1 | 生活・情報・キーボード | `life.g1.seikatsu`, `information.g1.jouhou`, `keyboard.g1.keyboard` | 自動採点＋既存活動 |
| 1 | 思考トレーニング | `thinking.g1.patterns-and-logic` | 自動採点 |
| 2 | 算数 | `math.g2.chart`, `math.g2.clock`, `math.g2.add`, `math.g2.sub`, `math.g2.measure`, `math.g2.hissan`, `math.g2.story`, `math.g2.kazu`, `math.g2.money`, `math.g2.order`, `math.g2.groups`, `math.g2.mul`, `math.g2.shape`, `math.g2.frac` | 自動採点 |
| 2 | 国語 | `japanese.g2.kokugo`, `japanese.g2.bun`, `japanese.g2.goi`, `japanese.g2.dokkai`, `japanese.g2.moji` | 自動採点 |
| 2 | 生活・情報・キーボード | `life.g2.seikatsu`, `information.g2.jouhou`, `keyboard.g2.keyboard` | 自動採点＋既存活動 |
| 2 | 思考トレーニング | `thinking.g2.patterns-and-space` | 自動採点 |
| 3 | 算数 | `math.g3.mul`, `math.g3.div`, `math.g3.shape`, `math.g3.hissan`, `math.g3.kazu`, `math.g3.soroban`, `math.g3.add`, `math.g3.sub`, `math.g3.clock`, `math.g3.measure`, `math.g3.story`, `math.g3.order`, `math.g3.chart`, `math.g3.frac`, `math.g3.money`, `math.g3.groups` | 自動採点 |
| 3 | 国語 | `japanese.g3.kokugo`, `japanese.g3.bun`, `japanese.g3.goi`, `japanese.g3.dokkai`, `japanese.g3.moji` | 自動採点 |
| 3 | 社会・理科・外国語 | `social.g3.shakai`, `science.g3.rika`, `english.g3.eigo` | 自動採点＋活動カード |
| 3 | 情報・総合・キーボード | `information.g3.jouhou`, `integrated.g3.sougou`, `keyboard.g3.keyboard` | 自動採点＋活動カード |
| 3 | 思考トレーニング | `thinking.g3.logic-and-strategy` | 自動採点 |

### 九九・かけ算とひっ算

`math.g2.mul` は `1×1` から `9×9` までの順序付き81事実を個別に記録する。難易度1～4で 2・5の段、3・4の段、6・7の段、8・9・1の段を導入し、難易度5で全段を混合する。各段階の全対象事実について少なくとも1回の自力正解を得るまで次へ進まず、援助・答え表示・誤答があった事実は定着度をリセットして再出題を優先する。

`math.g2.hissan` は二桁の繰り上がり・繰り下がりなしから始め、繰り上がり、繰り下がり、三桁、複数回の繰り上がり・繰り下がりへ進む。`math.g3.hissan` は三桁の加減、二桁×一桁、三桁×一桁、二桁×二桁、より大きな加減乗の混合へ進む。いずれも筆算の各列を順番に入力する自動採点形式である。

### 2年上ドリル対応

2年の算数単元は、市販の2年上ドリル（No.2〜44）に出る全問題型を生成する。Issue #85 の監査結果を次に示す。「既存」は出題済み、「追加」は本対応で生成器に加えた型である。

| ドリル | 問題型 | 対象単元 | 状態 |
|---|---|---|---|
| No.2 | 1年のふくしゅう（20までの加減・くり上がり/くり下がり） | `math.g2.add`, `math.g2.sub` | 既存（add stage1〜2、sub stage1〜2） |
| No.3 | 文章題：二桁±何十（22+10、33−10 など） | `math.g2.story` | 追加（二桁±何十のたし算・ひき算の文章題） |
| No.4 | ひょうとグラフ（ます目・1ますの単位・数量の比較） | `math.g2.chart` | 既存（絵グラフ・表の□・最大最小・差） |
| No.5 | 二桁＋一桁（16+4=20、47+5 など） | `math.g2.add` | 既存＋追加（stage3〜4 の二桁＋一桁に「答えがちょうど何十になる」型を追加） |
| No.6 | 何十−一桁（60−5）、二桁−一桁（くり下がり） | `math.g2.sub` | 既存＋追加（二桁−一桁は既存、何十−一桁の専用型を追加） |
| No.7〜8 | 時刻と時間：何分前・何分後、経過時間、午前・午後、1日=24時間・午前=12時間 | `math.g2.clock` | 既存＋追加（何分後・1日=24時間・午前=12時間は既存、何分前・経過時間を追加。午前/午後の よび方は 使うが 24時間制への かえ算は 出さない） |
| No.9〜10 | 長さ：cm/mm の読み取り、46mm=4cm6mm の換算、cm・mm の加減 | `math.g2.measure` | 既存＋追加（cm/mm・m/cm 換算は既存、mm→「XcmYmm」、cmYmm の加減を追加） |
| No.11〜21 | 筆算(1)：二桁±二桁、たしかめ算 | `math.g2.hissan` | 既存＋追加（二桁±二桁は stage1〜3 で既存、たしかめ算を追加） |
| No.22〜25 | 逆思考「はじめの数」「つかった数」（テープ図つき文章題） | `math.g2.story` | 既存＋追加（テープ図は既存、はじめの数・つかった数を求める逆思考型を追加） |
| No.26〜27 | 100をこえる数：かんすうじの読み、百・十・一の合成、10を何こあつめた数、1000の構成、数直線（1ずつ・10ずつ・100ずつ）、三桁の大小 | `math.g2.kazu`, `math.g2.order` | 既存＋追加（位の合成・つぎの数・10の何倍は既存、かんすうじ・1000の構成・数直線・三桁の比較を追加） |
| No.28〜29 | 何十・何百の加減（70+60、1000−600）、式と数の比較（60+70 □ 120） | `math.g2.add`, `math.g2.sub`, `math.g2.order` | 既存＋追加（何十同士（和が100以下）は既存、和が100をこえる何十・何百同士・式と数の比較を追加） |
| No.30〜31 | かさ：L/dL/mL の換算と L・dL の加減 | `math.g2.measure` | 既存＋追加（1L=10dL・1L=1000mL・1dL=100mL・XL→dL・XLdL→dL は既存、dL→LdL・L→mL・dL→mL・mL→LdL の換算と、dL同士・1L−dL・L・dLまじりの加減（4L2dL + 1L3dL、1L3dL - 3dL など）を追加） |
| No.34〜41 | 筆算(2)：答えが三桁の二桁＋二桁、百の位からのくり下がり（100−8、103−96）、三桁±一・二桁 | `math.g2.hissan` | 既存＋追加（三桁±三桁は stage4〜5 で既存、答えが三桁の二桁加算・三桁±一/二桁を追加） |
| No.42〜43 | 文章題：二桁＋二桁=三桁、まとめて考える（10人に4人と2人） | `math.g2.story` | 追加（二桁どうしをあわせる型・まとめてふえる型） |
| No.44 | しきと計算：（ ）を先に計算（16+(8+2) など） | `math.g2.add` | 追加（order 単元の類似型に加え、たし算単元にも導入） |

### SVG絵から漢字（小学1～3年）

小学1～3年の国語では、山・川・木などの固定語彙について、説明的なSVG図を先に提示し、対応する漢字を4択で選ぶ「絵から漢字」形式も出題する。図はReactの要素として安全に生成し、ラベルを aria-label 属性で公開する。保護者向け学習履歴JSONには、出題形式、図のID・ラベル、正答、選択肢、回答結果を保存し、SVGの生HTMLは保存しない。

## 4～6年の追加単元

「問題」は選択・並べ替え・短答等を画面上で自動採点する。「問題＋活動」は知識部分を自動採点し、観察、実験、会話、調理、裁縫、協働等を活動カードと自己振り返りで補完する。発音、実技、実験結果そのものの自動判定は行わない。

| 学年 | 教科 | 単元ID | 内容 | 形式・評価 | 根拠 |
|---|---|---|---|---|---|
| 4 | 算数 | `math.g4.number-calculation` | 大きな数・除法・小数・分数・概数 | 問題・自動 | 算数編 |
| 4 | 算数 | `math.g4.geometry-measurement` | 面積・角・四角形・直方体 | 問題・自動 | 算数編 |
| 4 | 算数 | `math.g4.relations-data` | 変化・表・折れ線グラフ・二次元表 | 問題・自動 | 算数編 |
| 5 | 算数 | `math.g5.number-calculation` | 整数の性質・小数の乗除・異分母分数 | 問題・自動 | 算数編 |
| 5 | 算数 | `math.g5.geometry-measurement` | 体積・合同・多角形・角柱 | 問題・自動 | 算数編 |
| 5 | 算数 | `math.g5.rate-statistics` | 平均・単位量・割合・円グラフ | 問題・自動 | 算数編 |
| 6 | 算数 | `math.g6.fraction-expression` | 分数の乗除・文字式 | 問題・自動 | 算数編 |
| 6 | 算数 | `math.g6.geometry-measurement` | 対称・拡大縮小・円の面積・柱体の体積 | 問題・自動 | 算数編 |
| 6 | 算数 | `math.g6.ratio-data` | 比・比例・データ分析 | 問題・自動 | 算数編 |
| 4 | 国語 | `japanese.g4.kanji-reading-writing` | 配当漢字202字の読む・書いて使う | 問題・自動 | 国語編 |
| 4 | 国語 | `japanese.g4.language-information` | 語彙・文法・情報・話す聞く・書く読む | 問題・自動 | 国語編 |
| 5 | 国語 | `japanese.g5.kanji-reading-writing` | 配当漢字193字の読む・書いて使う | 問題・自動 | 国語編 |
| 5 | 国語 | `japanese.g5.language-information` | 資料、引用、討論、構成 | 問題・自動 | 国語編 |
| 5 | 国語 | `japanese.g5.keigo-vocabulary` | 敬語・言葉の使い分け | 問題・自動 | 国語編 |
| 6 | 国語 | `japanese.g6.kanji-reading-writing` | 配当漢字191字の読む・書いて使う | 問題・自動 | 国語編 |
| 6 | 国語 | `japanese.g6.language-information` | 常体・敬体、複数資料の比較、提案・推敲 | 問題・自動 | 国語編 |
| 6 | 国語 | `japanese.g6.keigo-vocabulary` | 敬語・言葉の使い分け | 問題・自動 | 国語編 |
| 4 | 理科 | `science.g4.matter-energy` | 空気・水・温度・電流 | 問題＋活動 | 理科編 |
| 4 | 理科 | `science.g4.life-seasons` | 人の体・季節と生物 | 問題＋活動 | 理科編 |
| 4 | 理科 | `science.g4.earth-sky` | 天気・水の循環・月と星 | 問題＋活動 | 理科編 |
| 5 | 理科 | `science.g5.matter-energy` | 溶解・振り子・電磁石 | 問題＋活動 | 理科編 |
| 5 | 理科 | `science.g5.life-development` | 発芽・成長・結実・動物の誕生 | 問題＋活動 | 理科編 |
| 5 | 理科 | `science.g5.earth-weather` | 流れる水・天気の変化 | 問題＋活動 | 理科編 |
| 6 | 理科 | `science.g6.matter-energy` | 燃焼・水溶液・てこ・電気利用 | 問題＋活動 | 理科編 |
| 6 | 理科 | `science.g6.life-environment` | 人体・植物・生態系・環境 | 問題＋活動 | 理科編 |
| 6 | 理科 | `science.g6.earth-space` | 地層・土地の変化・月と太陽 | 問題＋活動 | 理科編 |
| 4 | 社会 | `social.g4.prefecture-services` | 都道府県・水・ごみ | 問題・自動 | 社会編 |
| 4 | 社会 | `social.g4.disaster-culture` | 災害・伝統文化・地域の発展 | 問題＋活動 | 社会編 |
| 4 | 社会 | `social.g4.local-inquiry` | 地域調査と資料活用 | 問題＋活動 | 社会編 |
| 5 | 社会 | `social.g5.land-food` | 国土・自然・食料生産 | 問題・自動 | 社会編 |
| 5 | 社会 | `social.g5.industry-information` | 工業・運輸・情報社会 | 問題・自動 | 社会編 |
| 5 | 社会 | `social.g5.environment-disaster` | 環境保全・自然災害 | 問題・自動 | 社会編 |
| 6 | 社会 | `social.g6.politics-constitution` | 憲法・政治・税 | 問題・自動 | 社会編 |
| 6 | 社会 | `social.g6.history` | 日本の歴史 | 問題・自動 | 社会編 |
| 6 | 社会 | `social.g6.international` | 国際社会と日本 | 問題・自動 | 社会編 |
| 4 | 外国語活動 | `english.g4.listen-speak` | 聞く・話す | 問題＋会話活動 | 外国語編 |
| 5 | 外国語 | `english.g5.five-domains-foundation` | 五領域の基礎 | 問題＋会話活動 | 外国語編 |
| 6 | 外国語 | `english.g6.five-domains-integration` | 五領域の活用 | 問題＋会話活動 | 外国語編 |
| 4～6 | 総合 | `integrated.g4.inquiry-cycle`, `integrated.g5.inquiry-cycle`, `integrated.g6.inquiry-cycle` | 課題設定・収集・整理・表現・振り返り | 問題＋活動 | 解説一覧 |
| 4～6 | 情報 | `information.g4.information-programming`, `information.g5.information-programming`, `information.g6.information-programming` | 出典・著作権・個人情報・データ・プログラミング | 問題＋活動 | 解説一覧 |

## 思考トレーニング（小学1～6年）

各学年に5段階、各段階に6種類×4問＝24問、合計720問を用意する。既存の小学1～3年の単元IDは維持し、小学4～6年を追加する。

| 学年 | 教科 | 単元ID | 内容 |
|---|---|---|---|
| 4 | 思考トレーニング | `thinking.g4.constraints-and-planning` | 条件整理・逆算・予算・作業順序・必勝手 |
| 5 | 思考トレーニング | `thinking.g5.evidence-and-combinations` | 複数条件・組合せの限定・同時進行の計画 |
| 6 | 思考トレーニング | `thinking.g6.deduction-and-strategy` | 情報不足の判断・多段階の逆算・最適化・相手の全応手を考える戦略 |

6種類は「ならびの推理」「わりあての推理」「うしろから考える」「道すじをたどる」「組合せをしぼる」「かならずを考える」。低学年は具体物と小さな数で考え、角度・文字式・抽象的な集合の知識を前提としない。高学年には予算の上限、同時進行できる作業と待つ作業、石取りの作戦を含める。「きめられない」を不正解の埋め草にせず、情報不足の問題では正解にし、解説で異なる2通りを示す。

解説は途中の手順と確かめ方を示す。テストは表示された問題文から条件を読み取り、並べ方・割当て・全組合せ・ゲームの全応手を別実装で検証する。問題数だけで教育効果を保証するものではない。

## 中学校1～3年の追加単元

内部学年は小1～小6を1～6、中1～中3を7～9として連続的に表す。社会は学校ごとの履修順の違いに対応するため、アプリ内の標準順を「中1：地理基礎・古代中世、中2：日本の諸地域・近世近代、中3：現代史・公民」とする。技術も3年間を基礎・発展・統合の順に並べる。実験、発音、製作、調理、協働は知識問題に活動カードと振り返りを組み合わせる。

| 学年 | 教科 | 単元ID（教科レーン内の順序） | 内容 | 形式・評価 | 根拠 |
|---|---|---|---|---|---|
| 中1 | 数学 | `math.g7.signed-numbers-expressions`, `math.g7.linear-equations`, `math.g7.proportion-functions`, `math.g7.plane-solid-data` | 正負の数、文字式、一次方程式、比例・反比例、図形、データ | 問題・自動 | 中学校 数学編 |
| 中2 | 数学 | `math.g8.algebra-simultaneous-equations`, `math.g8.linear-functions`, `math.g8.congruence-proof`, `math.g8.probability-distribution` | 式・連立方程式、一次関数、合同・証明、確率・分布 | 問題・自動 | 中学校 数学編 |
| 中3 | 数学 | `math.g9.expansion-roots`, `math.g9.quadratic-equations`, `math.g9.quadratic-functions`, `math.g9.similarity-circle-sampling` | 展開・因数分解・平方根、二次方程式、関数、相似・円・三平方・標本調査 | 問題・自動 | 中学校 数学編 |
| 中1 | 国語 | `japanese.g7.language-classics`, `japanese.g7.communication-reading-writing` | 言葉・文法・常用漢字・古典、話す聞く・書く読む | 問題＋対話 | 中学校 国語編 |
| 中2 | 国語 | `japanese.g8.language-classics`, `japanese.g8.communication-reading-writing` | 文脈・古典・常用漢字、論理・引用・議論 | 問題＋対話 | 中学校 国語編 |
| 中3 | 国語 | `japanese.g9.language-classics`, `japanese.g9.communication-reading-writing` | 言語文化、批評・複数資料・議論 | 問題＋対話 | 中学校 国語編 |
| 中1 | 理科 | `science.g7.classification`, `science.g7.matter`, `science.g7.light-sound-force`, `science.g7.earth` | 生物分類、物質、光音力、火山地震地層 | 問題＋実験活動 | 中学校 理科編 |
| 中2 | 理科 | `science.g8.body`, `science.g8.chemical-change`, `science.g8.electricity`, `science.g8.weather` | 人体、化学変化、電流磁界、気象 | 問題＋実験活動 | 中学校 理科編 |
| 中3 | 理科 | `science.g9.heredity`, `science.g9.ions`, `science.g9.motion-energy`, `science.g9.astronomy-environment` | 遺伝進化、イオン、運動エネルギー、宇宙環境 | 問題＋実験活動 | 中学校 理科編 |
| 中1 | 社会 | `social.g7.geography-foundations`, `social.g7.ancient-medieval-history` | 世界・日本の地域構成、古代・中世 | 問題・自動 | 中学校 社会編 |
| 中2 | 社会 | `social.g8.japan-regions`, `social.g8.early-modern-modern-history` | 日本の諸地域、近世・近代 | 問題・自動 | 中学校 社会編 |
| 中3 | 社会 | `social.g9.contemporary-history`, `social.g9.civics` | 現代史、憲法・政治・経済・国際 | 問題・自動 | 中学校 社会編 |
| 中1～3 | 外国語 | `english.g7.five-domains-foundation`, `english.g8.five-domains-development`, `english.g9.five-domains-integration` | 聞く・読む・話す（やり取り／発表）・書く | 問題＋会話活動 | 中学校 外国語編 |
| 中1～3 | 技術 | `technology.g7.materials-biological-foundation`, `technology.g8.energy-information`, `technology.g9.integrated-problem-solving` | 材料・生物育成、エネルギー・情報、統合的問題解決 | 問題＋製作活動 | 中学校 技術・家庭編 |
| 中1～3 | 総合 | `integrated.g7.inquiry-project`, `integrated.g8.inquiry-project`, `integrated.g9.inquiry-project` | 課題設定・調査・協働・表現・評価 | 問題＋探究活動 | 中学校 解説一覧 |
| 中1～3 | 情報 | `information.g7.data-programming-literacy`, `information.g8.data-programming-literacy`, `information.g9.data-programming-literacy` | 信頼性・権利・個人情報・データ・プログラミング | 問題＋制作活動 | 中学校 解説一覧 |

## 漢字・ローマ字

学年別漢字は80・160・200・202・193・191字、合計1,026字を収録する。起動時検査と生成監査で学年別字数、総数、重複を検証する。漢字は読みの問題と、文脈に応じて選んで書いて使う問題を分離する。ローマ字の表示・標準正答は令和7年内閣告示の現行表記に合わせ、タイピングでは同じ仮名を入力できる一般的なIME表記（例: `shi/si`, `chi/ti`, `tsu/tu`, `fu/hu`）も許容する。

## 自動検査

`KidsTraining.ArchitectureTests` は、単元IDの一意性、小学1年～中学3年の存在、必須メタデータ、前提関係の非循環性、教科レーンの包含、モード別の開始学年、学年単位の進級ゲート、対象外教科、漢字集合、全単元×全難易度の問題生成を検査する。生成問題は `unitId` と実単元学年を必須とし、未解放学年、正答重複、ゼロ除算、範囲外値、未登録生成器を拒否する。
