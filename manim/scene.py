from manim import *

DEFAULT_FONT_SIZE = 44

class ListVariables(Scene):
    def construct(self):
      tripValue = MathTex(r"tripValue", r"=", r"\$31.10", font_size=DEFAULT_FONT_SIZE)
      timeCost = MathTex(r"timeCost", r"=", r"\$37.20/hr", font_size=DEFAULT_FONT_SIZE)
      fare = MathTex(r"fare", r"=", r"\$12.00", font_size=DEFAULT_FONT_SIZE)
      time = MathTex(r"timeToArrival", r"=", r"21 \, min", font_size=DEFAULT_FONT_SIZE)
      time2 = MathTex(r"timeToArrival", r"=", r"0.35 \, hrs", font_size=DEFAULT_FONT_SIZE)
      # Left align the equations
      timeCost.next_to(tripValue, DOWN).align_to(tripValue, LEFT)
      fare.next_to(timeCost, DOWN).align_to(tripValue, LEFT)
      time.next_to(fare, DOWN).align_to(tripValue, LEFT)
      time2.next_to(fare, DOWN).align_to(tripValue, LEFT)
      self.play(Write(tripValue))
      self.play(Write(timeCost))
      self.play(Write(fare))
      self.play(Write(time))
      self.wait(1)
      self.play(ReplacementTransform(time, time2))
      self.wait(3)

class ListBusVariables(Scene):
    def construct(self):
      heading = Text("Best substitute: Bus", font_size=48).move_to(UP*2)
      self.play(FadeIn(heading))
      
      fare = MathTex(r"fare", r"=", r"\$2.50", font_size=DEFAULT_FONT_SIZE).shift(LEFT)
      time = MathTex(r"timeToArrival", r"=", r"40 \, min", font_size=DEFAULT_FONT_SIZE)
      time2 = MathTex(r"timeToArrival", r"=", r"0.67 \, hrs", font_size=DEFAULT_FONT_SIZE)
      # Left align the equations
      time.next_to(fare, DOWN).align_to(fare, LEFT)
      time2.next_to(fare, DOWN).align_to(fare, LEFT)
      self.play(Write(fare))
      self.play(Write(time))
      self.wait(1)
      self.play(ReplacementTransform(time, time2))
      self.wait(1)

class NetValueCalculation(Scene):
    def construct(self):
        equationStart = MathTex(r"netValueUber", r"=", r"tripValue", r"- fare", r"- (timeCost \cdot timeToArrival)", font_size=DEFAULT_FONT_SIZE).move_to(DOWN*2)
        equationWithValues = MathTex(r"=", r"\$31.10", r"- \$12.00", r"- (\$37.20/hr \cdot 0.35 \, hrs)", font_size=DEFAULT_FONT_SIZE)
        equationCombined = MathTex(r"=", r"\$31.10", r"- \$12.00", r"- \$13.02", font_size=DEFAULT_FONT_SIZE)
        equationSimplified = MathTex(r"=", r"\$6.08", font_size=DEFAULT_FONT_SIZE)
        finalResultAtTheTop = MathTex(r"netValueUber", r"=", r"\$6.08", font_size=DEFAULT_FONT_SIZE).move_to(UP*3)
        equationStart[0].set_color(YELLOW)
        finalResultAtTheTop[0].set_color(YELLOW)

        equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationCombined.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationSimplified.next_to(equationWithValues, DOWN).align_to(equationStart[1], LEFT)

        self.play(Write(equationStart[0]))
        self.play(Write(equationStart[1]))
        self.wait(1)
        self.play(Write(equationStart[2]))
        self.wait(1)
        self.play(Write(equationStart[3]))
        self.wait(1)
        self.play(Write(equationStart[4]))
        self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[1], equationWithValues[0]),
            TransformFromCopy(equationStart[2], equationWithValues[1]),
            TransformFromCopy(equationStart[3], equationWithValues[2]),
            TransformFromCopy(equationStart[4], equationWithValues[3]),
            lag_ratio=0.6,
            run_time=3
        ))
        self.wait(1)
        self.play(Transform(equationWithValues, equationCombined))
        self.wait(1)
        self.play(TransformFromCopy(equationCombined, equationSimplified))
        self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[0], finalResultAtTheTop[0]),
            TransformFromCopy(equationWithValues[0], finalResultAtTheTop[1]),
            TransformFromCopy(equationSimplified[1], finalResultAtTheTop[2]),
            lag_ratio=0.05,
            run_time=1
        ))
        self.wait(1)
        self.play(FadeOut(equationStart, equationWithValues, equationCombined, equationSimplified))
        self.wait(1)

class NetValueCalculationBus(Scene):
    def construct(self):
        equationStart = MathTex(r"netValueBus", r"=", r"tripValue", r"- fare", r"- (timeCost \cdot timeToArrival)", font_size=DEFAULT_FONT_SIZE).move_to(DOWN*2)
        equationWithValues = MathTex(r"=", r"\$31.10", r"- \$2.50", r"- (\$37.20/hr \cdot 0.67 \, hrs)", font_size=DEFAULT_FONT_SIZE)
        equationCombined = MathTex(r"=", r"\$31.10", r"- \$2.50", r"- \$24.80", font_size=DEFAULT_FONT_SIZE)
        equationSimplified = MathTex(r"=", r"\$3.80", font_size=DEFAULT_FONT_SIZE)

        finalResultAtTheTop = MathTex(r"netValueBus", r"=", r"\$3.80", font_size=DEFAULT_FONT_SIZE).move_to(UP*2.2)
        equationStart[0].set_color(YELLOW)
        finalResultAtTheTop[0].set_color(YELLOW)

        equationWithValues.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationCombined.next_to(equationStart, DOWN).align_to(equationStart[1], LEFT)
        equationSimplified.next_to(equationWithValues, DOWN).align_to(equationStart[1], LEFT)

        self.play(Write(equationStart))
        # self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[1], equationWithValues[0]),
            TransformFromCopy(equationStart[2], equationWithValues[1]),
            TransformFromCopy(equationStart[3], equationWithValues[2]),
            TransformFromCopy(equationStart[4], equationWithValues[3]),
            lag_ratio=0.6,
            run_time=1.5
        ))
        # self.wait(1)
        self.play(Transform(equationWithValues, equationCombined))
        # self.wait(1)
        self.play(TransformFromCopy(equationCombined, equationSimplified))
        self.wait(1)
        self.play(LaggedStart(
            TransformFromCopy(equationStart[0], finalResultAtTheTop[0]),
            TransformFromCopy(equationWithValues[0], finalResultAtTheTop[1]),
            TransformFromCopy(equationSimplified[1], finalResultAtTheTop[2]),
            lag_ratio=0.05,
            run_time=1
        ))
        self.wait(1)
        self.play(FadeOut(equationStart, equationWithValues, equationCombined, equationSimplified))
        self.wait(1)
class SurplusCalculation(Scene):
    def construct(self):
        
      surplus = MathTex(r"surplus", r"=", r"netValueUber", r"-", r"netValueBus", font_size=DEFAULT_FONT_SIZE)
      surplus[2].set_color(YELLOW)
      surplus[4].set_color(YELLOW)
      surplusValues = MathTex(r"=", r"\$6.08",  r"-", r"\$3.80", font_size=DEFAULT_FONT_SIZE).next_to(surplus, DOWN).align_to(surplus[1], LEFT)
      surplusValues[1].set_color(YELLOW)
      surplusValues[3].set_color(YELLOW)
      surplusValue = MathTex(r"=", r"\$2.28", font_size=DEFAULT_FONT_SIZE).next_to(surplus, DOWN).align_to(surplus[1], LEFT)
      surplusValue[1].set_color(YELLOW)
      self.play(Write(surplus))
      self.wait(1)
      self.play(Write(surplusValues))
      self.wait(1)
      self.play(ReplacementTransform(surplusValues, surplusValue))
      self.wait(1)
       