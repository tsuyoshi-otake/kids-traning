# 小学校1年～中学校3年カリキュラム対応表

この文書は、Kids Training の単元カタログと現行の小学校・中学校学習指導要領との対応を示す。実装上の唯一の正本は `CurriculumPolicy*.cs` の `CurriculumUnit` であり、全単元が安定した `unitId`、教科、領域、実学年、順序、前提単元、生成器、評価方式、根拠資料を持つ。

登録学校学年は保護者向けの表示・記録属性であり、出題上限ではない。通常は教科別に各教科の最初の単元から始まる。保護者が「登録学年の単元を優先する」をONにした場合だけ、進捗を変更せず、各教科で登録学年以上の最初の単元を開始位置として優先する。OFFへ戻すと未完了の低学年単元へ戻り、どちらの場合も登録学年より上へ自動進行できる。難易度5の合格によって定着確認へ入ると次単元が解放され、1日・3日・7日後の定着確認は後続単元を妨げない。

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

## 1～3年の既存単元

1～3年の既存問題は、次の規則で一問一答・選択・入力式の自動採点単元へ割り当てる。IDはすべて `教科.g学年.トピック` で、表中の並びが教科レーン内の順序になる。各IDの公式根拠は、算数・国語・社会・理科・外国語は上記の各教科解説、それ以外は学習指導要領・解説一覧である。

| 学年 | 教科 | 単元ID（順序） | 評価 |
|---|---|---|---|
| 1 | 算数 | `math.g1.kazu`, `math.g1.shape`, `math.g1.add`, `math.g1.sub`, `math.g1.clock`, `math.g1.measure`, `math.g1.story`, `math.g1.money`, `math.g1.groups`, `math.g1.chart` | 自動採点 |
| 1 | 国語 | `japanese.g1.moji`, `japanese.g1.bun`, `japanese.g1.kokugo`, `japanese.g1.goi`, `japanese.g1.dokkai` | 自動採点 |
| 1 | 生活・道徳・情報・特別活動・キーボード | `life.g1.seikatsu`, `moral.g1.doutoku`, `information.g1.jouhou`, `special-activities.g1.tokubetsu`, `keyboard.g1.keyboard` | 自動採点＋既存活動 |
| 2 | 算数 | `math.g2.chart`, `math.g2.clock`, `math.g2.add`, `math.g2.sub`, `math.g2.measure`, `math.g2.hissan`, `math.g2.story`, `math.g2.kazu`, `math.g2.money`, `math.g2.order`, `math.g2.groups`, `math.g2.mul`, `math.g2.shape`, `math.g2.frac` | 自動採点 |
| 2 | 国語 | `japanese.g2.kokugo`, `japanese.g2.bun`, `japanese.g2.goi`, `japanese.g2.dokkai`, `japanese.g2.moji` | 自動採点 |
| 2 | 生活・道徳・情報・特別活動・キーボード | `life.g2.seikatsu`, `moral.g2.doutoku`, `information.g2.jouhou`, `special-activities.g2.tokubetsu`, `keyboard.g2.keyboard` | 自動採点＋既存活動 |
| 3 | 算数 | `math.g3.mul`, `math.g3.div`, `math.g3.shape`, `math.g3.hissan`, `math.g3.kazu`, `math.g3.soroban`, `math.g3.add`, `math.g3.sub`, `math.g3.clock`, `math.g3.measure`, `math.g3.story`, `math.g3.order`, `math.g3.chart`, `math.g3.frac`, `math.g3.money`, `math.g3.groups` | 自動採点 |
| 3 | 国語 | `japanese.g3.kokugo`, `japanese.g3.bun`, `japanese.g3.goi`, `japanese.g3.dokkai`, `japanese.g3.moji` | 自動採点 |
| 3 | 社会・理科・外国語 | `social.g3.shakai`, `science.g3.rika`, `english.g3.eigo` | 自動採点＋活動カード |
| 3 | 道徳・情報・総合・特別活動・キーボード | `moral.g3.doutoku`, `information.g3.jouhou`, `integrated.g3.sougou`, `special-activities.g3.tokubetsu`, `keyboard.g3.keyboard` | 自動採点＋活動カード |

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
| 6 | 国語 | `japanese.g6.kanji-reading-writing` | 配当漢字191字の読む・書いて使う | 問題・自動 | 国語編 |
| 6 | 国語 | `japanese.g6.language-information` | 資料、引用、討論、構成 | 問題・自動 | 国語編 |
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
| 5 | 家庭科 | `home-economics.g5.family-food` | 家族・生活時間・調理・食品安全 | 問題＋活動 | 家庭編 |
| 5 | 家庭科 | `home-economics.g5.sewing-cleaning` | 裁縫・清掃・整理 | 問題＋活動 | 家庭編 |
| 6 | 家庭科 | `home-economics.g6.meal-clothing-housing` | 献立・衣食住の選択 | 問題＋活動 | 家庭編 |
| 6 | 家庭科 | `home-economics.g6.consumer-environment` | 買い物・消費者・環境 | 問題＋活動 | 家庭編 |
| 4～6 | 道徳 | `moral.g4.values-dialogue`, `moral.g5.values-dialogue`, `moral.g6.values-dialogue` | 学年帯の内容項目、多面的な対話 | 問題＋振り返り | 解説一覧 |
| 4～6 | 総合 | `integrated.g4.inquiry-cycle`, `integrated.g5.inquiry-cycle`, `integrated.g6.inquiry-cycle` | 課題設定・収集・整理・表現・振り返り | 問題＋活動 | 解説一覧 |
| 4～6 | 情報 | `information.g4.information-programming`, `information.g5.information-programming`, `information.g6.information-programming` | 出典・著作権・個人情報・データ・プログラミング | 問題＋活動 | 解説一覧 |
| 4～6 | 特別活動 | `special-activities.g4.school-role-career`, `special-activities.g5.school-role-career`, `special-activities.g6.school-role-career` | 学校生活・役割・キャリア | 問題＋振り返り | 解説一覧 |

## 中学校1～3年の追加単元

内部学年は小1～小6を1～6、中1～中3を7～9として連続的に表す。社会は学校ごとの履修順の違いに対応するため、アプリ内の標準順を「中1：地理基礎・古代中世、中2：日本の諸地域・近世近代、中3：現代史・公民」とする。技術・家庭も3年間を基礎・発展・統合の順に並べる。実験、発音、製作、調理、協働は知識問題に活動カードと振り返りを組み合わせる。

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
| 中1～3 | 家庭 | `home-economics.g7.family-food-foundation`, `home-economics.g8.clothing-housing-consumer`, `home-economics.g9.sustainable-family-project` | 家族・食、衣住・消費、持続可能な生活設計 | 問題＋実生活活動 | 中学校 技術・家庭編 |
| 中1～3 | 道徳 | `moral.g7.values-dialogue`, `moral.g8.values-dialogue`, `moral.g9.values-dialogue` | 多面的・多角的な判断と対話 | 問題＋振り返り | 中学校 解説一覧 |
| 中1～3 | 総合 | `integrated.g7.inquiry-project`, `integrated.g8.inquiry-project`, `integrated.g9.inquiry-project` | 課題設定・調査・協働・表現・評価 | 問題＋探究活動 | 中学校 解説一覧 |
| 中1～3 | 情報 | `information.g7.data-programming-literacy`, `information.g8.data-programming-literacy`, `information.g9.data-programming-literacy` | 信頼性・権利・個人情報・データ・プログラミング | 問題＋制作活動 | 中学校 解説一覧 |
| 中1～3 | 特別活動 | `special-activities.g7.school-career-citizenship`, `special-activities.g8.school-career-citizenship`, `special-activities.g9.school-career-citizenship` | 自治・役割・合意形成・キャリア | 問題＋振り返り | 中学校 解説一覧 |

## 漢字・ローマ字

学年別漢字は80・160・200・202・193・191字、合計1,026字を収録する。起動時検査と生成監査で学年別字数、総数、重複を検証する。漢字は読みの問題と、文脈に応じて選んで書いて使う問題を分離する。ローマ字の表示・標準正答は令和7年内閣告示の現行表記に合わせ、タイピングでは同じ仮名を入力できる一般的なIME表記（例: `shi/si`, `chi/ti`, `tsu/tu`, `fu/hu`）も許容する。

## 自動検査

`KidsTraining.ArchitectureTests` は、単元IDの一意性、小学1年～中学3年の存在、必須メタデータ、前提関係の非循環性、教科レーンの包含、対象外教科、漢字集合、全単元×全難易度の問題生成を検査する。生成問題は `unitId` と実単元学年を必須とし、正答重複、ゼロ除算、範囲外値、未登録生成器を拒否する。
