using System.Globalization;
using KidsTraining.App.Domain.Learning;

namespace KidsTraining.App.Application.Learning.Markup;

internal static partial class LearningMarkupPatcher
{
    private static string BuildProgressionScript()
    {
        var gradeOneLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(1));
        var gradeTwoLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(2));
        var gradeThreeLanes = ToJavaScriptNestedArray(CurriculumPolicy.TopicLanesForGrade(3));
        var reviewIntervals = string.Join(',', Enumerable.Range(0, ReviewSchedule.MaximumStep + 1)
            .Select(step => ((long)ReviewSchedule.IntervalAt(step).TotalMilliseconds).ToString(CultureInfo.InvariantCulture)));
        var requiredAccuracy = SkillEvidence.RequiredIndependentAccuracy.ToString(CultureInfo.InvariantCulture);
        var requiredConfidence = SkillEvidence.RequiredConfidence.ToString(CultureInfo.InvariantCulture);

        return $$$"""
skillAverage(p){const values=Object.values((p&&p.mastery)||{}).map(v=>Number(v)).filter(v=>Number.isFinite(v));return values.length?values.reduce((a,b)=>a+b,0)/values.length:0.05;}
  pickStage(stage,buckets,reviewRate=.25){const top=Math.max(1,Math.min(5,Number(stage)||1));let current=top-1;while(current>0&&!(buckets[current]&&buckets[current].length))current--;const active=(buckets[current]||[]).map(fn=>({fn:fn,difficulty:current+1})),previous=[];for(let i=0;i<current;i++)for(const fn of (buckets[i]||[]))previous.push({fn:fn,difficulty:i+1});const pool=previous.length&&Math.random()<reviewRate?previous:(active.length?active:previous);if(!pool.length)throw new Error('No questions configured for stage '+top);const picked=pool[this.rand(0,pool.length-1)],q=picked.fn();q.difficulty=picked.difficulty;return q;}
  effectiveGrade(p){return Math.max(1,Math.min(3,Number(p&&p.grade)||1));}
  gradeLabel(p){return this.effectiveGrade(p)+'年生';}
  learningStage(p){const level=this.skillLevel(p),stars=Number(p.stars)||0;if(stars<15&&level<=1)return 1;if(stars<45||level<=2)return 2;if(stars<90||level<=3)return 3;if(stars<150||level<=4)return 4;return 5;}
  topicStage(p,k){const s=p&&p.skillStats&&p.skillStats[k],saved=Number(s&&s.level);if(Number.isFinite(saved))return Math.max(1,Math.min(5,saved));const m=Number((p&&p.mastery&&p.mastery[k])||0.05);return Math.max(1,Math.min(5,Math.floor(m*5)+1));}
  reviewStage(p,k){const stage=this.topicStage(p,k);return this.topicDue(p,k)&&stage>1?Math.max(1,stage-1):stage;}
  profileAtStage(p,k,stage){const level=Math.max(1,Math.min(5,stage)),levels=[0.05,0.25,0.45,0.65,0.85],mastery={...((p&&p.mastery)||{})},skillStats={...((p&&p.skillStats)||{})};mastery[k]=levels[level-1];skillStats[k]={...(skillStats[k]||{}),level:level};return{...p,mastery:mastery,skillStats:skillStats};}
  ensureLearningProfile(p){const keys=Object.keys(this.topics);if(p.learningSchema===3&&p.mastery&&p.skillStats&&p.cleared&&keys.every(k=>p.skillStats[k]&&Number.isFinite(Number(p.skillStats[k].level))))return p;p.mastery=p.mastery||{};p.skillStats=p.skillStats||{};p.cleared=p.cleared||{};for(const k of keys){const m=this.clamp(Number(p.mastery[k])||0.05,0.05,0.99);p.mastery[k]=m;const old=p.skillStats[k]||{},mastered=Number(old.masteredAt)||(p.cleared[k]?Number(old.lastAttemptAt)||Date.now():null),derived=mastered?5:Math.max(1,Math.min(5,Math.floor(m*5)+1));p.skillStats[k]={attempts:Number(old.attempts)||0,independent:Number(old.independent)||0,assisted:Number(old.assisted)||0,revealed:Number(old.revealed)||0,errors:Number(old.errors)||0,confidence:this.clamp(Number(old.confidence)||m,0.05,0.99),reviewStep:Math.max(0,Math.min(3,Number(old.reviewStep)||0)),lastAttemptAt:Number(old.lastAttemptAt)||null,nextReviewAt:Number(old.nextReviewAt)||null,masteredAt:mastered,level:Math.max(1,Math.min(5,Number(old.level)||derived)),stageAttempts:Number(old.stageAttempts)||0,stageIndependent:Number(old.stageIndependent)||0};}p.learningSchema=3;return p;}
  migrateProfiles(profiles){return (Array.isArray(profiles)?profiles:[]).map(p=>this.ensureLearningProfile({...p,mastery:{...(p.mastery||{})},skillStats:{...(p.skillStats||{})},cleared:{...(p.cleared||{})}}));}
  topicStat(p,k){this.ensureLearningProfile(p);return p.skillStats[k];}
  topicComplete(p,k){return !!this.topicStat(p,k).masteredAt;}
  topicDue(p,k,now=Date.now()){const n=this.topicStat(p,k).nextReviewAt;return !!n&&n<=now;}
  topicReady(p,k,now=Date.now()){const s=this.topicStat(p,k),accuracy=s.attempts?s.independent/s.attempts:0;return s.level>=5&&s.independent>={{{SkillEvidence.RequiredIndependentCorrect}}}&&s.attempts>={{{SkillEvidence.RequiredAttempts}}}&&accuracy>={{{requiredAccuracy}}}&&s.confidence>={{{requiredConfidence}}}&&!this.topicDue(p,k,now);}
  markCleared(p,k,now=Date.now()){const s=this.topicStat(p,k);if(this.topicReady(p,k,now)&&!s.masteredAt){s.masteredAt=now;(p.cleared=p.cleared||{})[k]=true;}}
  recordEvidence(p,q,outcome){const s=this.topicStat(p,q.topic),now=Date.now(),intervals=[{{{reviewIntervals}}}],difficulty=Math.max(1,Math.min(5,Number(q.difficulty)||s.level)),aligned=difficulty===s.level;s.attempts++;s.lastAttemptAt=now;if(aligned)s.stageAttempts++;if(outcome==='independent'){s.independent++;if(aligned)s.stageIndependent++;s.confidence=this.clamp(s.confidence+.12,.05,.99);if(!s.nextReviewAt||s.nextReviewAt<=now){s.nextReviewAt=now+intervals[Math.max(0,Math.min(3,s.reviewStep))];s.reviewStep=Math.min(3,s.reviewStep+1);}}else if(outcome==='assisted'){s.assisted++;s.confidence=this.clamp(s.confidence-.03,.05,.99);s.nextReviewAt=now;s.reviewStep=0;}else if(outcome==='revealed'){s.revealed++;s.errors++;s.confidence=this.clamp(s.confidence-.08,.05,.99);s.nextReviewAt=now;s.reviewStep=0;}else{s.errors++;s.confidence=this.clamp(s.confidence-.10,.05,.99);s.nextReviewAt=now;s.reviewStep=0;}if(s.level<5&&s.stageAttempts>=3&&s.stageIndependent/s.stageAttempts>=.67){s.level++;s.stageAttempts=0;s.stageIndependent=0;}p.mastery[q.topic]=s.confidence;this.markCleared(p,q.topic,now);const sess=this.state.session;if(sess){if(outcome==='independent')sess.correct++;if(q.topic===sess.targetTopic){sess.targetAsked++;if(outcome==='independent')sess.targetIndependent++;}}return outcome;}
  hissanComplete(p){return this.topicReady(p,'hissan');}
  curriculumLanes(p){const g=this.effectiveGrade(p),g1={{{gradeOneLanes}}},g2={{{gradeTwoLanes}}},g3={{{gradeThreeLanes}}},raw=g===1?g1:(g===2?g2:g3),cfg=this.state.settings,configured=cfg&&cfg.topics;return raw.map(lane=>configured?lane.filter(k=>configured[k]!==false):lane.slice()).filter(lane=>lane.length);}
  gradeTopics(p){const out=[];for(const lane of this.curriculumLanes(p))for(const k of lane)if(!out.includes(k))out.push(k);return out;}
  introducedTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){let frontier=lane.findIndex(k=>!this.topicComplete(p,k));if(frontier<0)frontier=lane.length-1;for(let i=0;i<=frontier;i++)if(!out.includes(lane[i]))out.push(lane[i]);}for(const k of this.gradeTopics(p)){const s=this.topicStat(p,k);if((s.attempts>0||s.masteredAt)&&!out.includes(k))out.push(k);}return out;}
  allowedTopics(p){const introduced=this.introducedTopics(p);return introduced.length?introduced:this.gradeTopics(p);}
  frontierTopics(p){const out=[];for(const lane of this.curriculumLanes(p)){const next=lane.find(k=>!this.topicComplete(p,k));if(next&&!out.includes(next))out.push(next);}return out.length?out:this.allowedTopics(p);}
  laneProgress(p,k){const lane=this.curriculumLanes(p).find(x=>x.includes(k))||[k],done=lane.filter(x=>this.topicComplete(p,x)).length;return done/Math.max(1,lane.length);}
  nextCurriculumTopic(p){const front=this.frontierTopics(p);return front.slice().sort((a,b)=>this.laneProgress(p,a)-this.laneProgress(p,b))[0]||this.weakestTopic(p,this.allowedTopics(p));}
  dueTopics(p,now=Date.now()){return this.allowedTopics(p).filter(k=>this.topicDue(p,k,now));}
  weightedPick(p,pool){const ks=pool&&pool.length?pool:this.allowedTopics(p),w=ks.map(k=>.25+(1-this.topicStat(p,k).confidence)*1.75+(this.topicDue(p,k)?1:0));let sum=w.reduce((a,b)=>a+b,0),r=Math.random()*sum;for(let i=0;i<ks.length;i++){r-=w[i];if(r<=0)return ks[i];}return ks[0];}
  weakestTopic(p,pool){const ks=pool&&pool.length?pool:this.allowedTopics(p);return ks.slice().sort((a,b)=>this.topicStat(p,a).confidence-this.topicStat(p,b).confidence||this.topicStat(p,a).independent-this.topicStat(p,b).independent)[0];}
""";
    }

    private static string ToJavaScriptArray(IEnumerable<string> values) =>
        "[" + string.Join(',', values.Select(value => "'" + value + "'")) + "]";

    private static string ToJavaScriptNestedArray(IEnumerable<IReadOnlyList<string>> values) =>
        "[" + string.Join(',', values.Select(ToJavaScriptArray)) + "]";
}
